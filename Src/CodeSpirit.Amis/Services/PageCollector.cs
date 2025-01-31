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

        public PageCollector(
            IOptions<PagesConfiguration> pagesConfig,
            ILogger<PageCollector> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IValidator<Page> pageValidator,
            ApplicationPartManager applicationPartManager)
        {
            _pagesConfig = pagesConfig;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _pageValidator = pageValidator;
            this.applicationPartManager = applicationPartManager;
        }

        public async Task<Dictionary<string, Page>> CollectPagesAsync()
        {
            var pageDict = new Dictionary<string, Page>();
            var controllerTypes = GetPublicNonAbstractControllers();

            foreach (var controller in controllerTypes)
            {
                // 处理控制器级别的 PageAttribute
                AddPageIfAttributeExists(pageDict, controller.GetCustomAttribute<PageAttribute>(), controller);

                // 处理动作方法级别的 PageAttribute
                var actionPages = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => m.IsDefined(typeof(HttpMethodAttribute), inherit: true))
                    .Select(m => m.GetCustomAttribute<PageAttribute>())
                    .Where(attr => attr != null)
                    .Select(attr => _mapper.Map<Page>(attr))
                    .ToList();

                foreach (var page in actionPages)
                {
                    SetPagePropertiesFromController(page, controller);
                    ValidateAndAddPage(pageDict, page);
                }
            }

            // 处理从配置文件中加载的页面信息
            var configPages = _pagesConfig.Value.Pages ?? Enumerable.Empty<ConfigurationPage>();
            var mappedConfigPages = _mapper.Map<IEnumerable<Page>>(configPages);
            foreach (var page in mappedConfigPages)
            {
                ValidateAndAddPage(pageDict, page);
            }

            // 模拟异步操作（如果需要，可以实际执行异步操作）
            await Task.CompletedTask;

            return pageDict;
        }

        private void AddPageIfAttributeExists(Dictionary<string, Page> pageDict, PageAttribute attr, Type controller = null)
        {
            if (attr == null) return;

            var page = _mapper.Map<Page>(attr);
            if (controller != null)
            {
                SetPagePropertiesFromController(page, controller);
            }

            ValidateAndAddPage(pageDict, page);
        }

        private void SetPagePropertiesFromController(Page page, Type controller)
        {
            if (string.IsNullOrEmpty(page.Url))
            {
                var controllerName = controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                page.Url = $"/{controllerName}";
            }

            if (string.IsNullOrEmpty(page.SchemaApi) && page.Schema == null && string.IsNullOrEmpty(page.Redirect))
            {
                var controllerName = controller.Name.Replace("Controller", "", StringComparison.OrdinalIgnoreCase);
                var request = _httpContextAccessor.HttpContext?.Request;
                if (request != null)
                {
                    var host = request.Host.Value;
                    var scheme = request.Scheme;
                    var route = GetRoute(controller, controllerName).Replace("api/", "api/amis/");
                    page.SchemaApi = $"{scheme}://{host}/" + route;
                }
            }
        }

        private void ValidateAndAddPage(Dictionary<string, Page> pageDict, Page page)
        {
            ValidationResult result = _pageValidator.Validate(page);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
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

            pageDict[page.Label] = page;
        }

        public List<Type> GetPublicNonAbstractControllers()
        {
            var controllerFeature = new ControllerFeature();
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
