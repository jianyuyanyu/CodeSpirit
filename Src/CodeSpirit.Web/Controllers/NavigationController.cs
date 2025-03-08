using CodeSpirit.Core.Extensions;
using CodeSpirit.Navigation;
using CodeSpirit.Navigation.Models;
using CodeSpirit.Navigation.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 导航控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NavigationController : ControllerBase
    {
        private const string DefaultDashboardUrl = "/";
        private const string DefaultDashboardIcon = "fa-solid fa-gauge-high";
        private readonly INavigationService _navigationService;
        private readonly ILogger<NavigationController> _logger;
        private readonly DashboardConfig _dashboardConfig;

        public NavigationController(
            INavigationService navigationService,
            ILogger<NavigationController> logger)
        {
            _navigationService = navigationService;
            _logger = logger;
        }

        /// <summary>
        /// 获取导航树（Page格式）
        /// </summary>
        /// <returns>Page格式的导航树JSON</returns>
        [HttpGet("site")]
        public async Task<ActionResult<object>> GetNavigationPageTree()
        {
            var tree = await _navigationService.GetNavigationTreeAsync();
            var pageTree = ConvertToPageFormat(tree).ToList();

            if (pageTree.Any())
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
            return new
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