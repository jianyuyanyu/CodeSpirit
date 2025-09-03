
using CodeSpirit.AiFormFill.Models;

namespace CodeSpirit.AiFormFill.Services;

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
                
                _endpoints[endpointInfo.Route] = endpointInfo;
                
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
        var route = GenerateApiRoute(dtoType, aiFormFillAttr);
        
        return new AiFormFillEndpointInfo
        {
            DtoType = dtoType,
            Route = route,
            TriggerField = aiFormFillAttr.TriggerField,
            Attribute = aiFormFillAttr
        };
    }

    /// <summary>
    /// 生成API路由
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <param name="aiFormFillAttr">AI填充特性</param>
    /// <returns>API路由</returns>
    private string GenerateApiRoute(Type dtoType, AiFormFillAttribute aiFormFillAttr)
    {
        var serviceName = InferServiceName(dtoType);
        var controllerName = InferControllerName(dtoType);
        var endpoint = string.IsNullOrEmpty(aiFormFillAttr.ApiEndpoint) || aiFormFillAttr.ApiEndpoint == "ai-fill" 
            ? "ai-fill" 
            : aiFormFillAttr.ApiEndpoint;

        if (string.IsNullOrEmpty(serviceName))
        {
            // 默认路径
            return $"/api/{controllerName.ToLower()}/{endpoint}";
        }

        // 标准API路径
        return $"/api/{serviceName}/{controllerName.ToLower()}/{endpoint}";
    }

    /// <summary>
    /// 推断控制器名称
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <returns>控制器名称</returns>
    private string InferControllerName(Type dtoType)
    {
        var typeName = dtoType.Name;
        
        // 移除常见的DTO后缀
        if (typeName.EndsWith("Request"))
            typeName = typeName.Substring(0, typeName.Length - 7);
        else if (typeName.EndsWith("Dto"))
            typeName = typeName.Substring(0, typeName.Length - 3);
        
        // 移除Create、Generate、Add等前缀
        if (typeName.StartsWith("Create"))
            typeName = typeName.Substring(6);
        else if (typeName.StartsWith("Generate"))
            typeName = typeName.Substring(8);
        else if (typeName.StartsWith("Add"))
            typeName = typeName.Substring(3);
        
        // 转换为复数形式（简单规则）
        if (!typeName.EndsWith("s"))
        {
            if (typeName.EndsWith("y"))
                typeName = typeName.Substring(0, typeName.Length - 1) + "ies";
            else
                typeName += "s";
        }
        
        return typeName;
    }

    /// <summary>
    /// 推断服务名称
    /// </summary>
    /// <param name="dtoType">DTO类型</param>
    /// <returns>服务名称</returns>
    private string InferServiceName(Type dtoType)
    {
        var namespaceParts = dtoType.Namespace?.Split('.') ?? Array.Empty<string>();
        var apiServicePart = namespaceParts.FirstOrDefault(part => part.EndsWith("Api"));
        
        if (string.IsNullOrEmpty(apiServicePart))
            return null;
            
        // 移除CodeSpirit.前缀和Api后缀，转换为小写
        return apiServicePart
            .Replace("CodeSpirit.", "")
            .Replace("Api", "")
            .ToLowerInvariant();
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
    public AiFormFillEndpointInfo? FindEndpointByRoute(string route)
    {
        return _endpoints.Values.FirstOrDefault(e => 
            string.Equals(e.Route, route, StringComparison.OrdinalIgnoreCase));
    }
}
