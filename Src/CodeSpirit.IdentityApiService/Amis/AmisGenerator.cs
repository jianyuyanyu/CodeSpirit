using CodeSpirit.IdentityApi.Amis.Helpers;
using CodeSpirit.IdentityApi.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.IdentityApi.Amis
{
    /// <summary>
    /// 用于生成 AMIS（百度前端框架）所需的 JSON 配置的生成器类。
    /// </summary>
    public partial class AmisGenerator
    {
        private readonly Assembly _assembly;
        private readonly CachingHelper _cachingHelper;
        private readonly ControllerHelper _controllerHelper;
        private readonly CrudHelper _crudHelper;
        private readonly AmisConfigBuilder _amisConfigBuilder;
        private readonly PermissionService _permissionService;

        /// <summary>
        /// 构造函数，初始化依赖项。
        /// </summary>
        /// <param name="assembly">包含控制器的程序集。</param>
        /// <param name="httpContextAccessor">用于访问当前 HTTP 上下文。</param>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="cache">内存缓存，用于缓存生成的 AMIS JSON。</param>
        public AmisGenerator(Assembly assembly, IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, IMemoryCache cache)
        {
            _assembly = assembly;
            _permissionService = (PermissionService)permissionService;
            _cachingHelper = new CachingHelper(httpContextAccessor, cache);
            _controllerHelper = new ControllerHelper(assembly);
            var utilityHelper = new UtilityHelper();

            _crudHelper = new CrudHelper();
            var apiRouteHelper = new ApiRouteHelper(_controllerHelper, httpContextAccessor);
            var columnHelper = new ColumnHelper(_permissionService, utilityHelper);
            var buttonHelper = new ButtonHelper(_permissionService, null, null); // 根据需要传递必要的参数
            var searchFieldHelper = new SearchFieldHelper(_permissionService, utilityHelper);
            var formFieldHelper = new FormFieldHelper(_permissionService, utilityHelper);
            _amisConfigBuilder = new AmisConfigBuilder(apiRouteHelper, columnHelper, buttonHelper, searchFieldHelper, formFieldHelper, _permissionService);
        }

        /// <summary>
        /// 生成指定控制器的 AMIS JSON 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 "Controller" 后缀）。</param>
        /// <returns>AMIS 定义的 JSON 对象，如果控制器不存在或不支持则返回 null。</returns>
        public JObject GenerateAmisJsonForController(string controllerName)
        {
            var cacheKey = _cachingHelper.GenerateCacheKey(controllerName);
            if (_cachingHelper.TryGetValue(cacheKey, out JObject cachedAmisJson))
            {
                return cachedAmisJson;
            }

            var controllerType = _controllerHelper.GetControllerType(controllerName);
            if (controllerType == null)
                return null;

            var actions = _crudHelper.HasCrudActions(controllerType);
            if (actions.Create == null || actions.Read == null || actions.Update == null || actions.Delete == null)
                return null;

            var crudConfig = _amisConfigBuilder.GenerateAmisCrudConfig(controllerName, controllerType, actions);
            if (crudConfig != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cachingHelper.Set(cacheKey, crudConfig, cacheEntryOptions);
            }

            return crudConfig;
        }
    }
}

