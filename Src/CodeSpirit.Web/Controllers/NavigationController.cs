using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.Enums;
using CodeSpirit.Navigation;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 导航控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(policy: "DynamicPermissions")]
    public class NavigationController : ControllerBase
    {
        private const string DefaultDashboardUrl = "/";
        private const string DefaultDashboardIcon = "fa-solid fa-gauge-high";
        private readonly INavigationService _navigationService;
        private readonly ILogger<NavigationController> _logger;
        private readonly IHasPermissionService _hasPermissionService;
        private readonly DashboardConfig _dashboardConfig;

        public NavigationController(
            INavigationService navigationService,
            ILogger<NavigationController> logger,
            IHasPermissionService hasPermissionService)
        {
            _navigationService = navigationService;
            _logger = logger;
            _hasPermissionService = hasPermissionService;
        }

        /// <summary>
        /// 获取导航树（Page格式）
        /// </summary>
        /// <param name="platformType">平台类型，默认为Both</param>
        /// <param name="deviceType">设备类型，默认为desktop</param>
        /// <param name="groupFilter">分组过滤器</param>
        /// <param name="includeDashboard">是否包含控制台首页，默认为true</param>
        /// <returns>Page格式的导航树JSON</returns>
        [HttpGet("site")]
        public async Task<ActionResult<object>> GetNavigationPageTree(
            [FromQuery] PlatformType platformType = PlatformType.Both,
            [FromQuery] string deviceType = "desktop",
            [FromQuery] string[] groupFilter = null,
            [FromQuery] bool includeDashboard = true)
        {
            try
            {
                var tree = await _navigationService.GetNavigationTreeAsync(platformType);
                
                // 创建上下文过滤条件
                var filterContext = new NavigationFilterContext
                {
                    PlatformType = platformType,
                    PermissionService = _hasPermissionService,
                    DeviceType = deviceType,
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    IsDevelopment = IsEnvironmentDevelopment(),
                    GroupFilter = groupFilter ?? [],
                    UserTags = GetUserTags()
                };

                // 使用新的上下文过滤功能
                var filteredNodes = _navigationService.FilterNodesByContext(tree, filterContext);
                var pageTree = ConvertToPageFormat(filteredNodes).ToList();

                if (pageTree.Any() && includeDashboard)
                {
                    var dashboardConfig = new DashboardConfig
                    {
                        Label = "控制台",
                        Url = DefaultDashboardUrl,
                        Icon = DefaultDashboardIcon
                    };
                    pageTree.Insert(0, dashboardConfig.ToDashboardNode());
                }

                return new { Pages = new { Children = pageTree } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation page tree for platform {PlatformType}, device {DeviceType}", platformType, deviceType);
                return StatusCode(500, new { Message = "获取导航树失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 获取租户平台导航树（Page格式）
        /// </summary>
        /// <param name="tenantId">租户ID</param>
        /// <param name="deviceType">设备类型，默认为desktop</param>
        /// <param name="groupFilter">分组过滤器</param>
        /// <param name="includeDashboard">是否包含控制台首页，默认为true</param>
        /// <returns>Page格式的导航树JSON</returns>
        [HttpGet("tenant")]
        public async Task<ActionResult<object>> GetTenantNavigationPageTree(
            [FromQuery] string tenantId,
            [FromQuery] string deviceType = "desktop",
            [FromQuery] string[] groupFilter = null,
            [FromQuery] bool includeDashboard = true)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                {
                    return BadRequest(new { Message = "租户ID不能为空" });
                }

                // 获取租户平台的导航
                var tree = await _navigationService.GetNavigationTreeAsync(PlatformType.Tenant);
                
                // 创建租户上下文过滤条件
                var filterContext = new NavigationFilterContext
                {
                    PlatformType = PlatformType.Tenant,
                    PermissionService = _hasPermissionService,
                    DeviceType = deviceType,
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    IsDevelopment = IsEnvironmentDevelopment(),
                    GroupFilter = groupFilter ?? [],
                    UserTags = GetUserTags()
                };

                // 使用新的上下文过滤功能
                var filteredNodes = _navigationService.FilterNodesByContext(tree, filterContext);
                
                // 使用标准的页面格式转换，无需特殊处理租户路径
                var pageTree = ConvertToPageFormat(filteredNodes).ToList();

                if (pageTree.Any() && includeDashboard)
                {
                    var dashboardConfig = new DashboardConfig
                    {
                        Label = "控制台",
                        Url = DefaultDashboardUrl,
                        Icon = DefaultDashboardIcon
                    };
                    pageTree.Insert(0, dashboardConfig.ToDashboardNode());
                }

                return new { Pages = new { Children = pageTree } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tenant navigation page tree for tenant {TenantId}, device {DeviceType}", tenantId, deviceType);
                return StatusCode(500, new { Message = "获取租户导航树失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 获取指定平台的导航树（原始格式）
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <returns>原始格式的导航树</returns>
        [HttpGet("tree")]
        public async Task<ActionResult<List<NavigationNode>>> GetNavigationTree(
            [FromQuery] PlatformType platformType = PlatformType.Both)
        {
            try
            {
                var tree = await _navigationService.GetNavigationTreeAsync(platformType);
                var filteredNodes = _navigationService.FilterNodesByPermission(tree, _hasPermissionService);
                return Ok(filteredNodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get navigation tree for platform {PlatformType}", platformType);
                return StatusCode(500, new { Message = "获取导航树失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 清除导航缓存
        /// </summary>
        /// <param name="moduleName">模块名称，为空时清除所有缓存</param>
        /// <param name="platformType">平台类型，为空时清除所有平台缓存</param>
        /// <returns></returns>
        [HttpDelete("cache")]
        public async Task<ActionResult> ClearNavigationCache(
            [FromQuery] string moduleName = null,
            [FromQuery] PlatformType? platformType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(moduleName))
                {
                    await _navigationService.ClearAllNavigationCacheAsync();
                    _logger.LogInformation("All navigation cache cleared by user {User}", User.Identity?.Name);
                }
                else
                {
                    await _navigationService.ClearModuleNavigationCacheAsync(moduleName, platformType);
                    _logger.LogInformation("Navigation cache cleared for module {ModuleName}, platform {PlatformType} by user {User}", 
                        moduleName, platformType, User.Identity?.Name);
                }

                return Ok(new { Message = "缓存清除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear navigation cache for module {ModuleName}, platform {PlatformType}", moduleName, platformType);
                return StatusCode(500, new { Message = "清除缓存失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 重新初始化导航树
        /// </summary>
        /// <returns></returns>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeNavigationTree()
        {
            try
            {
                await _navigationService.InitializeNavigationTree();
                _logger.LogInformation("Navigation tree initialized by user {User}", User.Identity?.Name);
                return Ok(new { Message = "导航树初始化成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize navigation tree");
                return StatusCode(500, new { Message = "导航树初始化失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 检查是否为开发环境
        /// </summary>
        /// <returns></returns>
        private bool IsEnvironmentDevelopment()
        {
            // 可以通过配置或环境变量来判断
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }

        /// <summary>
        /// 获取用户标签
        /// </summary>
        /// <returns></returns>
        private string[] GetUserTags()
        {
            // 可以从用户Claims、数据库或其他地方获取用户标签
            // 这里简单示例，可以根据实际需求扩展
            var tags = new List<string>();
            
            if (User.IsInRole("Admin"))
                tags.Add("admin");
            if (User.IsInRole("Manager"))
                tags.Add("management");
            if (User.IsInRole("User"))
                tags.Add("user");

            return tags.ToArray();
        }

        private static IEnumerable<object> ConvertToPageFormat(IEnumerable<NavigationNode> nodes, NavigationNode parent = null)
        {
            if (nodes == null || !nodes.Any()) return null;

            return nodes
                .Where(node => IsValidNode(node, parent))
                .Select(node => CreatePageNode(node));
        }

        private static bool IsValidNode(NavigationNode node, NavigationNode parent)
        {
            return !string.IsNullOrEmpty(node.Title) &&
                   ((parent == null && node.Children != null && node.Children.Any()) || parent != null);
        }

        private static object CreatePageNode(NavigationNode node)
        {
            var pageNode = new
            {
                label = node.Title,
                url = node.Path,
                link = node.Link,
                icon = node.Icon,
                permission = node.Permission,
                children = ConvertToPageFormat(node.Children, node),
                schemaApi = GetSchemaApi(node),
                schema = GetScheme(node)
            };

            // 添加扩展属性到页面节点
            if (!string.IsNullOrEmpty(node.Badge))
            {
                return new
                {
                    pageNode.label,
                    pageNode.url,
                    pageNode.link,
                    pageNode.icon,
                    pageNode.permission,
                    pageNode.children,
                    pageNode.schemaApi,
                    pageNode.schema,
                    badge = new
                    {
                        text = node.Badge,
                        type = node.BadgeType
                    }
                };
            }

            return pageNode;
        }

        private static object GetScheme(NavigationNode node)
        {
            if (node.IsExternal && node.Target == "_self")
            {
                return new
                {
                    type = "page",
                    body = new
                    {
                        type = "iframe",
                        src = node.Link,
                        height = "100%"
                    }
                };
            }
            return null;
        }

        private static string GetSchemaApi(NavigationNode node)
        {
            if (node.IsExternal) return null;
            //针对web模块的特殊处理，直接返回options:/{route}?amis格式
            if (node.ModuleName.Equals("web", StringComparison.CurrentCultureIgnoreCase))
            {
                return !string.IsNullOrEmpty(node.Route) ? $"options:/{node.Route}?amis" : null;
            }
            return !string.IsNullOrEmpty(node.Route) ? $"options:/{node.ModuleName.ToCamelCase()}/{node.Route}?amis" : null;
        }
    }

    public class DashboardConfig
    {
        public string Label { get; set; } = "控制台";
        public string Url { get; set; } = "/";
        public string Icon { get; set; } = "fa-solid fa-gauge-high";
        public string MarkdownContent { get; set; } = @"## 框架概览

CodeSpirit（码灵）是一款革命性的全栈低代码开发框架，通过智能代码生成引擎与AI深度协同，实现**后端驱动式全栈开发范式**。基于.NET 9技术栈构建，将具备企业级技术深度与云原生扩展能力，提供从界面生成、业务逻辑编排到系统运维的全生命周期支持。
- Github：[xin-lai/CodeSpirit](https://github.com/xin-lai/CodeSpirit)
- Gitee：[magicodes/CodeSpirit](https://gitee.com/magicodes/code-spirit)";

        public object ToDashboardNode()
        {
            return new
            {
                label = Label,
                url = Url,
                icon = Icon,
                schema = new
                {
                    type = "page",
                    //body = new
                    //{
                    //    type = "markdown",
                    //    value = MarkdownContent,
                    //    options = new
                    //    {
                    //        html = true,
                    //        linkify = true,
                    //        breaks = false
                    //    }
                    //}
                    body = new
                    {
                        type = "service",
                        schemaApi = "options:/identity/api/identity/userStatistics?amis=&_replace=1",
                    }
                }
            };
        }
    }
}