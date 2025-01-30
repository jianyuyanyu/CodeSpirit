using CodeSpirit.IdentityApi.Amis.App;
using CodeSpirit.IdentityApi.Amis.Attributes;
using CodeSpirit.IdentityApi.Amis.Configuration;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis
{
    /// <summary>
    /// 提供站点配置相关的服务，包括处理控制器和动作方法上的页面属性，
    /// 以及从配置文件中加载页面信息。
    /// </summary>
    public class SiteConfigurationService : ISiteConfigurationService
    {
        private readonly IOptions<PagesConfiguration> _pagesConfig;
        private readonly ILogger<SiteConfigurationService> _logger;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// 构造函数，注入配置和日志记录服务。
        /// </summary>
        /// <param name="pagesConfig">页面配置选项。</param>
        /// <param name="logger">日志记录器。</param>
        public SiteConfigurationService(
            IOptions<PagesConfiguration> pagesConfig,
            ILogger<SiteConfigurationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _pagesConfig = pagesConfig;
            _logger = logger;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取站点的整体配置，包括页面层级结构等信息。
        /// </summary>
        /// <returns>包含站点配置的 API 响应。</returns>
        public ApiResponse<App.App> GetSiteConfiguration()
        {
            var pages = new List<Page>();

            // 初始化站点配置响应对象
            var site = new ApiResponse<App.App>
            {
                Status = 0,
                Msg = "",
                Data = new App.App
                {
                    Pages = new List<PageGroup>
                    {
                        new PageGroup
                        {
                            Children = pages
                        }
                    }
                }
            };

            // 获取所有公开的非抽象控制器类型
            var controllerTypes = GetPublicNonAbstractControllers();

            // 使用字典来存储页面，键为页面的标签，以便快速查找和避免重复
            var pageDict = new Dictionary<string, Page>();

            // 遍历每个控制器，处理控制器和动作方法上的页面属性
            foreach (var controller in controllerTypes)
            {
                // 处理控制器级别的 PageAttribute
                var controllerAttr = controller.GetCustomAttribute<PageAttribute>();
                if (controllerAttr != null)
                {
                    var controllerPage = CreatePageFromAttribute(controllerAttr, controller);
                    AddPage(pageDict, pages, controllerPage);
                }

                // 处理动作方法级别的 PageAttribute
                var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsPublic && m.IsDefined(typeof(HttpMethodAttribute), inherit: true))
                    .ToList();

                foreach (var action in actions)
                {
                    var actionAttr = action.GetCustomAttribute<PageAttribute>();
                    if (actionAttr != null)
                    {
                        var actionPage = CreatePageFromAttribute(actionAttr);
                        AddPage(pageDict, pages, actionPage);
                    }
                }
            }

            // 处理从配置文件中加载的页面信息
            if (_pagesConfig.Value.Pages != null && _pagesConfig.Value.Pages.Any())
            {
                foreach (var configPage in _pagesConfig.Value.Pages)
                {
                    var configPageObj = CreatePageFromConfig(configPage);
                    AddPage(pageDict, pages, configPageObj);
                }
            }

            return site;
        }

        #region Helper Methods

        /// <summary>
        /// 获取当前程序集中的所有公开的、非抽象的控制器类型。
        /// </summary>
        /// <returns>控制器类型列表。</returns>
        private List<Type> GetPublicNonAbstractControllers()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type)
                               && !type.IsAbstract
                               && type.IsPublic)
                .ToList();
        }

        /// <summary>
        /// 根据 PageAttribute 创建页面对象。
        /// </summary>
        /// <param name="attr">页面属性。</param>
        /// <returns>页面对象。</returns>
        private Page CreatePageFromAttribute(PageAttribute attr, Type controller = null)
        {
            var page = new Page
            {
                Label = attr.Label,
                Url = attr.Url,
                Redirect = attr.Redirect,
                SchemaApi = attr.SchemaApi,
                ParentLabel = attr.ParentLabel,
                Icon = attr.Icon,
            };

            if (controller != null)
            {
                if (string.IsNullOrEmpty(page.Url))
                {
                    var controllerName = controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                    page.Url = $"/{controllerName}";
                }

                if (string.IsNullOrEmpty(page.SchemaApi) && page.Schema == null && string.IsNullOrEmpty(page.Redirect))
                {
                    var controllerName = controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                    var request = httpContextAccessor.HttpContext?.Request;
                    var host = request.Host.Value;
                    var scheme = request.Scheme;
                    page.SchemaApi = $"{scheme}://{host}/" + GetRoute(controller, controllerName).Replace("api/", "api/amis/");
                }
            }

            // 如果属性中包含 Schema，则尝试反序列化
            if (!string.IsNullOrEmpty(attr.Schema))
            {
                page.Schema = TryDeserializeSchema(attr.Schema);
            }

            return page;
        }

        public string GetRoute(Type controller, string name)
        {
            var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
            return routeAttr?.Template?.Replace("[controller]", name) ?? string.Empty;
        }

        /// <summary>
        /// 根据配置文件中的页面信息创建页面对象。
        /// </summary>
        /// <param name="configPage">配置页面对象。</param>
        /// <returns>页面对象。</returns>
        private Page CreatePageFromConfig(ConfigurationPage configPage)
        {
            var page = new Page
            {
                Label = configPage.Label,
                Url = configPage.Url,
                Redirect = configPage.Redirect,
                SchemaApi = configPage.SchemaApi,
                ParentLabel = configPage.ParentLabel,
                Schema = configPage.Schema,
                Children = configPage.Children
            };
            return page;
        }

        /// <summary>
        /// 尝试将 JSON 字符串反序列化为 Schema 对象。
        /// </summary>
        /// <param name="schemaJson">Schema 的 JSON 表示。</param>
        /// <returns>反序列化后的 Schema 对象，如果失败则为 null。</returns>
        private Schema? TryDeserializeSchema(string schemaJson)
        {
            try
            {
                return JsonConvert.DeserializeObject<Schema>(schemaJson);
            }
            catch (JsonException ex)
            {
                // 反序列化失败时记录错误日志
                _logger.LogError(ex, "Failed to deserialize schema: {SchemaJson}", schemaJson);
                return null;
            }
        }

        /// <summary>
        /// 将页面添加到页面字典中，并处理其父子关系。
        /// </summary>
        /// <param name="pageDict">页面字典。</param>
        /// <param name="topLevelPages">顶级页面列表。</param>
        /// <param name="page">要添加的页面。</param>
        private void AddPage(Dictionary<string, Page> pageDict, List<Page> topLevelPages, Page page)
        {
            if (!pageDict.ContainsKey(page.Label))
            {
                pageDict.Add(page.Label, page);
            }
            else
            {
                // 如果存在重复的标签，可以选择记录警告或处理冲突
                _logger.LogWarning("Duplicate page label detected: {Label}. Ignoring duplicate.", page.Label);
                return;
            }

            if (!string.IsNullOrEmpty(page.ParentLabel))
            {
                // 处理子页面
                if (pageDict.TryGetValue(page.ParentLabel, out var parentPage))
                {
                    parentPage.Children ??= new List<Page>();
                    parentPage.Children.Add(page);
                }
                else
                {
                    // 如果父页面不存在，则创建新的父页面并添加到顶级页面列表中
                    var newParentPage = new Page
                    {
                        Label = page.ParentLabel,
                        Children = new List<Page> { page }
                    };

                    pageDict.Add(newParentPage.Label, newParentPage);
                    topLevelPages.Add(newParentPage);

                    _logger.LogInformation("Created new parent page: {ParentLabel} for child page: {ChildLabel}", newParentPage.Label, page.Label);
                }
            }
            else
            {
                // 处理顶级页面
                topLevelPages.Add(page);
            }
        }

        #endregion
    }
}
