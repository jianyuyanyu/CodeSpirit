using CodeSpirit.Amis.Helpers;
using CodeSpirit.Core.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis
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
        private readonly IServiceProvider serviceProvider;
        private readonly AmisContext _amisContext;

        /// <summary>
        /// 构造函数，初始化依赖项。
        /// </summary>
        /// <param name="assembly">包含控制器的程序集。</param>
        /// <param name="httpContextAccessor">用于访问当前 HTTP 上下文。</param>
        /// <param name="permissionService">权限服务，用于检查用户权限。</param>
        /// <param name="cache">内存缓存，用于缓存生成的 AMIS JSON。</param>
        public AmisGenerator(IHttpContextAccessor httpContextAccessor, IPermissionService permissionService, AmisContext amisContext, CachingHelper cachingHelper, ControllerHelper controllerHelper, CrudHelper crudHelper, IServiceProvider serviceProvider, Assembly assembly)
        {
            _assembly = assembly;
            _amisContext = amisContext;
            _amisContext.Assembly = _assembly;
            _cachingHelper = cachingHelper;
            _controllerHelper = controllerHelper;
            _crudHelper = crudHelper;
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 生成指定控制器的 AMIS JSON 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 "Controller" 后缀）。</param>
        /// <returns>AMIS 定义的 JSON 对象，如果控制器不存在或不支持则返回 null。</returns>
        public JObject GenerateAmisJsonForController(string controllerName)
        {
            _amisContext.ControllerName = controllerName; // 在方法内设置 controllerName

            string cacheKey = _cachingHelper.GenerateCacheKey(controllerName);
            if (_cachingHelper.TryGetValue(cacheKey, out JObject cachedAmisJson))
            {
                return cachedAmisJson;
            }

            Type controllerType = _controllerHelper.GetControllerType(controllerName);
            if (controllerType == null)
                return null;
            _amisContext.ControllerType = controllerType;

            CrudActions actions = _crudHelper.HasCrudActions(controllerType);
            _amisContext.Actions = actions;

            AmisConfigBuilder _amisConfigBuilder = serviceProvider.GetRequiredService<AmisConfigBuilder>();
            JObject crudConfig = _amisConfigBuilder.GenerateAmisCrudConfig();
            if (crudConfig != null)
            {
                MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cachingHelper.Set(cacheKey, crudConfig, cacheEntryOptions);
            }

            return crudConfig;
        }
    }
}

