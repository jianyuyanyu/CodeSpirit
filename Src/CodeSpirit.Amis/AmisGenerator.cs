using CodeSpirit.Amis.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CodeSpirit.Amis
{
    /// <summary>
    /// AMIS 配置生成器，用于生成 AMIS（百度前端框架）所需的 JSON 配置。
    /// 支持 CRUD 操作配置和统计图表配置的生成。
    /// </summary>
    public partial class AmisGenerator
    {
        private readonly Assembly _assembly;
        private readonly CachingHelper _cachingHelper;
        private readonly ControllerHelper _controllerHelper;
        private readonly CrudHelper _crudHelper;
        private readonly IServiceProvider _serviceProvider;
        private readonly AmisContext _amisContext;
        private readonly StatisticsConfigBuilder _statisticsBuilder;

        /// <summary>
        /// 初始化 AMIS 配置生成器的新实例。
        /// </summary>
        /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
        /// <param name="amisContext">AMIS 上下文</param>
        /// <param name="cachingHelper">缓存帮助类</param>
        /// <param name="controllerHelper">控制器帮助类</param>
        /// <param name="crudHelper">CRUD 操作帮助类</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="assembly">包含控制器的程序集</param>
        public AmisGenerator(
            IHttpContextAccessor httpContextAccessor,
            AmisContext amisContext,
            CachingHelper cachingHelper,
            ControllerHelper controllerHelper,
            CrudHelper crudHelper,
            IServiceProvider serviceProvider,
            Assembly assembly)
        {
            _assembly = assembly;
            _amisContext = amisContext;
            _amisContext.Assembly = _assembly;
            _cachingHelper = cachingHelper;
            _controllerHelper = controllerHelper;
            _crudHelper = crudHelper;
            _serviceProvider = serviceProvider;
            _statisticsBuilder = new StatisticsConfigBuilder(_controllerHelper);
        }

        /// <summary>
        /// 为指定的控制器生成 AMIS CRUD 配置。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 Controller 后缀）</param>
        /// <returns>AMIS CRUD 配置的 JSON 对象，如果控制器不存在则返回 null</returns>
        public JObject GenerateAmisJsonForController(string controllerName)
        {
            _amisContext.ControllerName = controllerName;

            Type controllerType = GetAndValidateControllerType(controllerName);
            return controllerType == null ? null : GetOrGenerateCrudConfig(controllerType, controllerName);
        }

        /// <summary>
        /// 为指定的控制器生成统计图表配置。
        /// </summary>
        /// <param name="controllerName">控制器名称（不含 Controller 后缀）</param>
        /// <returns>统计图表配置的 JSON 对象，如果控制器不存在或没有统计方法则返回 null</returns>
        public JObject GenerateStatisticsAmisJson(string controllerName)
        {
            Type controllerType = GetAndValidateControllerType(controllerName);
            return controllerType == null ? null : _statisticsBuilder.BuildStatisticsConfig(controllerType);
        }

        /// <summary>
        /// 获取并验证控制器类型。
        /// </summary>
        /// <param name="controllerName">控制器名称</param>
        /// <returns>控制器类型，如果不存在则返回 null</returns>
        private Type GetAndValidateControllerType(string controllerName)
        {
            Type controllerType = _controllerHelper.GetControllerType(controllerName);
            if (controllerType == null)
            {
                return null;
            }

            _amisContext.ControllerType = controllerType;
            return controllerType;
        }

        /// <summary>
        /// 获取或生成 CRUD 配置，支持缓存机制。
        /// </summary>
        /// <param name="controllerType">控制器类型</param>
        /// <param name="controllerName">控制器名称</param>
        /// <returns>CRUD 配置的 JSON 对象</returns>
        private JObject GetOrGenerateCrudConfig(Type controllerType, string controllerName)
        {
            string cacheKey = _cachingHelper.GenerateCacheKey(controllerName);

            // 尝试从缓存获取配置
            if (_cachingHelper.TryGetValue(cacheKey, out JObject cachedAmisJson))
            {
                return cachedAmisJson;
            }

            _amisContext.Actions = _crudHelper.HasCrudActions(controllerType);

            // 生成新的配置
            AmisConfigBuilder amisConfigBuilder = _serviceProvider.GetRequiredService<AmisConfigBuilder>();
            JObject crudConfig = amisConfigBuilder.GenerateAmisCrudConfig();

            // 如果生成成功，则缓存配置
            if (crudConfig != null)
            {
                MemoryCacheEntryOptions cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                _cachingHelper.Set(cacheKey, crudConfig, cacheOptions);
            }

            return crudConfig;
        }
    }
}

