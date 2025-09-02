using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Helpers;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// AI表单填充端点扫描器
/// 自动扫描所有标记了AiFormFillAttribute的DTO类型
/// </summary>
public class AiFormFillEndpointScanner : ISingletonDependency
{
    private readonly ILogger<AiFormFillEndpointScanner> _logger;
    private readonly Dictionary<string, AiFormFillEndpointInfo> _endpoints;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AiFormFillEndpointScanner(ILogger<AiFormFillEndpointScanner> logger)
    {
        _logger = logger;
        _endpoints = new Dictionary<string, AiFormFillEndpointInfo>();
    }

    /// <summary>
    /// 扫描指定程序集中的AI填充DTO
    /// </summary>
    /// <param name="assemblies">要扫描的程序集</param>
    public void ScanAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly);
        }
    }

    /// <summary>
    /// 扫描单个程序集
    /// </summary>
    /// <param name="assembly">程序集</param>
    private void ScanAssembly(Assembly assembly)
    {
        try
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<AiFormFillAttribute>() != null);

            foreach (var type in types)
            {
                var aiFormFillAttr = type.GetCustomAttribute<AiFormFillAttribute>()!;
                var endpointInfo = CreateEndpointInfo(type, aiFormFillAttr);
                
                var key = $"{endpointInfo.ControllerName}:{endpointInfo.DtoType.Name}";
                _endpoints[key] = endpointInfo;
                
                _logger.LogInformation("发现AI填充DTO: {DtoType} -> {Route}", 
                    type.Name, endpointInfo.Route);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫描程序集 {AssemblyName} 时发生错误", assembly.FullName);
        }
    }

    /// <summary>
    /// 创建端点信息
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <param name="aiFormFillAttr">AI填充特性</param>
    /// <returns>端点信息</returns>
    private AiFormFillEndpointInfo CreateEndpointInfo(Type dtoType, AiFormFillAttribute aiFormFillAttr)
    {
        var controllerName = AiFormFillRouteHelper.InferControllerName(dtoType);
        var route = AiFormFillRouteHelper.GenerateApiRoute(dtoType, aiFormFillAttr);
        
        return new AiFormFillEndpointInfo
        {
            DtoType = dtoType,
            ControllerName = controllerName,
            Route = route,
            TriggerField = aiFormFillAttr.TriggerField,
            ApiEndpoint = aiFormFillAttr.ApiEndpoint,
            MaxTokens = aiFormFillAttr.MaxTokens,
            EnableCache = aiFormFillAttr.EnableCache,
            CacheExpirationMinutes = aiFormFillAttr.CacheExpirationMinutes
        };
    }



    /// <summary>
    /// 获取所有扫描到的端点
    /// </summary>
    /// <returns>端点信息集合</returns>
    public IReadOnlyDictionary<string, AiFormFillEndpointInfo> GetEndpoints()
    {
        return _endpoints.AsReadOnly();
    }

    /// <summary>
    /// 根据路由查找端点信息
    /// </summary>
    /// <param name="route">路由</param>
    /// <returns>端点信息，如果未找到则返回null</returns>
    public AiFormFillEndpointInfo FindEndpointByRoute(string route)
    {
        return _endpoints.Values.FirstOrDefault(e => 
            string.Equals(e.Route, route, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// AI表单填充端点信息
/// </summary>
public class AiFormFillEndpointInfo
{
    /// <summary>
    /// DTO类型
    /// </summary>
    public Type DtoType { get; set; } = null!;

    /// <summary>
    /// 控制器名称
    /// </summary>
    public string ControllerName { get; set; } = string.Empty;

    /// <summary>
    /// 路由
    /// </summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>
    /// 触发字段
    /// </summary>
    public string TriggerField { get; set; } = string.Empty;

    /// <summary>
    /// API端点
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 最大Token数量
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
}
