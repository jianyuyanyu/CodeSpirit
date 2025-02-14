using AutoMapper;
using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Configuration;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeSpirit.Amis.Services
{
    public class PageCollector : IPageCollector
    {
        private readonly IOptions<PagesConfiguration> _pagesConfig;
        private readonly ILogger<PageCollector> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly IValidator<Page> _pageValidator;
        private readonly ApplicationPartManager applicationPartManager;
        private readonly IHasPermissionService _permissionService;

        public PageCollector(
            IOptions<PagesConfiguration> pagesConfig,
            ILogger<PageCollector> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IValidator<Page> pageValidator,
            ApplicationPartManager applicationPartManager,
            IHasPermissionService permissionService)
        {
            _pagesConfig = pagesConfig;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _pageValidator = pageValidator;
            this.applicationPartManager = applicationPartManager;
            _permissionService = permissionService;
        }

        public async Task<Dictionary<string, Page>> CollectPagesAsync()
        {
            Dictionary<string, Page> pageDict = [];
            List<Type> controllerTypes = GetPublicNonAbstractControllers();

            foreach (Type controller in controllerTypes)
            {
                // 处理控制器级别的 PageAttribute
                AddPageIfAttributeExists(pageDict, controller.GetCustomAttribute<PageAttribute>(), controller);

                // 处理动作方法级别的 PageAttribute
                List<Page> actionPages = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsDefined(typeof(HttpMethodAttribute), inherit: true))
                    .Select(m => m.GetCustomAttribute<PageAttribute>())
                    .Where(attr => attr != null)
                    .Select(attr => _mapper.Map<Page>(attr))
                    .ToList();

                foreach (Page page in actionPages)
                {
                    SetPagePropertiesFromController(page, controller);
                    ValidateAndAddPage(pageDict, page);
                }
            }

            // 处理从配置文件中加载的页面信息
            IEnumerable<ConfigurationPage> configPages = _pagesConfig.Value.Pages ?? Enumerable.Empty<ConfigurationPage>();
            IEnumerable<Page> mappedConfigPages = _mapper.Map<IEnumerable<Page>>(configPages);
            foreach (Page page in mappedConfigPages)
            {
                ValidateAndAddPage(pageDict, page);
            }

            // 模拟异步操作（如果需要，可以实际执行异步操作）
            await Task.CompletedTask;

            return pageDict;
        }

        private void AddPageIfAttributeExists(Dictionary<string, Page> pageDict, PageAttribute attr, Type controller = null)
        {
            if (attr == null)
            {
                return;
            }

            Page page = _mapper.Map<Page>(attr);
            if (controller != null)
            {
                SetPagePropertiesFromController(page, controller);
            }

            ValidateAndAddPage(pageDict, page);
        }

        private void SetPagePropertiesFromController(Page page, Type controller)
        {
            string controllerName = controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(page.Url))
            {
                page.Url = controllerName.ToCamelCase();
            }

            if (string.IsNullOrEmpty(page.SchemaApi) && page.Schema == null && string.IsNullOrEmpty(page.Redirect))
            {
                HttpRequest request = _httpContextAccessor.HttpContext?.Request;
                if (request != null)
                {
                    string host = request.Host.Value;
                    string scheme = _pagesConfig.Value.ForceHttps ? "https" : request.Scheme;
                    string route = GetRoute(controller, controllerName);
                    page.SchemaApi = $"options:{scheme}://{host}/{route}?amis";
                }
            }
        }

        private void ValidateAndAddPage(Dictionary<string, Page> pageDict, Page page)
        {
            // 首先验证权限
            if (!string.IsNullOrEmpty(page.PermissionCode) && !_permissionService.HasPermission(page.PermissionCode))
            {
                _logger.LogDebug("Page '{Label}' skipped due to insufficient permissions", page.Label);
                return;
            }

            ValidationResult result = _pageValidator.Validate(page);
            if (!result.IsValid)
            {
                foreach (ValidationFailure failure in result.Errors)
                {
                    _logger.LogWarning("Validation failed for page '{Label}': {Error}", page.Label, failure.ErrorMessage);
                }
                return;
            }

            if (pageDict.ContainsKey(page.Label))
            {
                _logger.LogWarning("Duplicate page label detected: {Label}. Ignoring duplicate.", page.Label);
                return;
            }

            // 如果页面有子页面，递归检查子页面的权限
            if (page.Children?.Any() == true)
            {
                page.Children = page.Children
                    .Where(child => string.IsNullOrEmpty(child.PermissionCode) ||
                                   _permissionService.HasPermission(child.PermissionCode))
                    .ToList();

                // 如果过滤后没有子页面了，且该页面本身没有其他内容（schema/schemaApi/redirect），则不添加该页面
                if (!page.Children.Any() &&
                    page.Schema == null &&
                    string.IsNullOrEmpty(page.SchemaApi) &&
                    string.IsNullOrEmpty(page.Redirect))
                {
                    return;
                }
            }

            pageDict[page.Label] = page;
        }

        public List<Type> GetPublicNonAbstractControllers()
        {
            ControllerFeature controllerFeature = new();
            applicationPartManager.PopulateFeature(controllerFeature);
            return controllerFeature.Controllers
                                     .Select(c => c.AsType())
                                     .Where(type => !type.IsAbstract && type.IsPublic)
                                     .ToList();
        }

        private string GetRoute(Type controller, string name) =>
            controller.GetCustomAttribute<RouteAttribute>()?.Template?.Replace("[controller]", name) ?? string.Empty;
    }
}
