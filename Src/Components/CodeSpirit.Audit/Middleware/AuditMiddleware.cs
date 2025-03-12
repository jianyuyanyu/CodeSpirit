using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Core.Attributes;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Abstractions;
using MvcControllerActionDescriptor = Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 控制器操作描述符
/// </summary>
public class AuditControllerActionDescriptor
{
    /// <summary>
    /// 控制器名称
    /// </summary>
    public string ControllerName { get; set; }
    
    /// <summary>
    /// 操作名称
    /// </summary>
    public string ActionName { get; set; }
    
    /// <summary>
    /// 控制器类型信息
    /// </summary>
    public TypeInfo ControllerTypeInfo { get; set; }
    
    /// <summary>
    /// 方法信息
    /// </summary>
    public MethodInfo MethodInfo { get; set; }
}

/// <summary>
/// 审计中间件
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly AuditOptions _options;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger,
        IConfiguration configuration,
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null)
    {
        _next = next;
        _logger = logger;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options;
        
        // 初始化控制器类型缓存
        if (actionDescriptorCollectionProvider != null)
        {
            InitializeControllerTypesCache(actionDescriptorCollectionProvider);
        }
    }
    
    /// <summary>
    /// 初始化控制器类型缓存
    /// </summary>
    private void InitializeControllerTypesCache(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        try
        {
            var actionDescriptors = actionDescriptorCollectionProvider.ActionDescriptors.Items;
            foreach (var descriptor in actionDescriptors)
            {
                // 使用明确的完全限定类型名称
                if (descriptor is MvcControllerActionDescriptor controllerActionDescriptor)
                {
                    var controllerName = controllerActionDescriptor.ControllerName;
                    var controllerType = controllerActionDescriptor.ControllerTypeInfo.AsType();
                    
                    _controllerTypeCache.TryAdd(controllerName, controllerType);
                }
            }
            
            _logger.LogInformation("已从应用程序部件初始化控制器类型缓存，共 {Count} 个控制器", _controllerTypeCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化控制器类型缓存失败");
        }
    }
    
    /// <summary>
    /// 处理请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IAuditService auditService, IGeoLocationService geoLocationService)
    {
        // 检查是否启用审计
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }
        
        // 检查请求路径是否需要排除
        var requestPath = context.Request.Path.Value;
        if (ShouldSkipAudit(context))
        {
            await _next(context);
            return;
        }
        
        // 开始计时
        var stopwatch = Stopwatch.StartNew();
        
        // 获取控制器和方法信息
        var endpoint = context.GetEndpoint();
        AuditControllerActionDescriptor controllerActionDescriptor = null;
        
        if (endpoint != null)
        {
            // 尝试从Endpoint获取控制器和操作信息
            controllerActionDescriptor = ExtractControllerActionDescriptorFromEndpoint(endpoint);
            
            // 如果无法从Endpoint获取，则退回到路由数据方法
            if (controllerActionDescriptor == null)
            {
                // 从路由数据中提取控制器和操作信息
                var routeData = context.GetRouteData();
                if (routeData != null && 
                    routeData.Values.TryGetValue("controller", out var controllerName) && 
                    routeData.Values.TryGetValue("action", out var actionName))
                {
                    var controllerStr = controllerName?.ToString();
                    var actionStr = actionName?.ToString();
                    
                    if (!string.IsNullOrEmpty(controllerStr) && !string.IsNullOrEmpty(actionStr))
                    {
                        // 尝试获取控制器类型
                        var controllerType = FindControllerType(controllerStr);
                        if (controllerType != null)
                        {
                            // 尝试获取操作方法
                            var methodInfo = FindActionMethod(controllerType, actionStr);
                            if (methodInfo != null)
                            {
                                controllerActionDescriptor = new AuditControllerActionDescriptor
                                {
                                    ControllerName = controllerStr,
                                    ActionName = actionStr,
                                    ControllerTypeInfo = controllerType.GetTypeInfo(),
                                    MethodInfo = methodInfo
                                };
                            }
                        }
                    }
                }
            }
        }
        
        // 保存原始请求体
        var originalRequestBody = await GetRequestBodyAsync(context);
        
        // 记录响应
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        
        // 获取客户端IP地址
        var ipAddress = GetClientIpAddress(context);
        
        // 获取用户代理信息
        var userAgent = GetUserAgent(context);
        
        var auditLog = new Models.AuditLog
        {
            RequestPath = context.Request.GetDisplayUrl(),
            RequestMethod = context.Request.Method,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestParams = _options.LogRequestParams ? SanitizeSensitiveData(originalRequestBody) : null
        };
        
        var isSuccess = true;
        var errorMessage = string.Empty;
        
        try
        {
            // 提取用户信息
            if (context.User.Identity?.IsAuthenticated == true)
            {
                auditLog.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                auditLog.UserName = context.User.FindFirstValue(ClaimTypes.Name);
            }
            
            // 如果不记录匿名请求且用户未认证，则跳过审计
            if (!_options.LogAnonymousRequests && string.IsNullOrEmpty(auditLog.UserId))
            {
                await _next(context);
                return;
            }
            
            // 调用下一个中间件
            await _next(context);
            
            // 检查响应状态
            if (context.Response.StatusCode >= 400)
            {
                isSuccess = false;
                errorMessage = $"HTTP Error: {context.Response.StatusCode}";
            }
            
            // 如果不记录未授权请求且响应状态为401或403，则跳过审计
            if (!_options.LogUnauthorizedRequests && (context.Response.StatusCode == 401 || context.Response.StatusCode == 403))
            {
                return;
            }
            
            // 提取控制器和方法信息
            if (controllerActionDescriptor != null)
            {
                var controllerType = controllerActionDescriptor.ControllerTypeInfo;
                var actionMethodInfo = controllerActionDescriptor.MethodInfo;
                
                auditLog.ControllerName = controllerActionDescriptor.ControllerName;
                auditLog.ActionName = controllerActionDescriptor.ActionName;
                auditLog.ServiceName = controllerType.Assembly.GetName().Name;
                
                // 获取控制器和方法上的审计特性
                var controllerAuditAttr = controllerType.GetCustomAttribute<AuditAttribute>();
                var methodAuditAttr = actionMethodInfo.GetCustomAttribute<AuditAttribute>();
                
                // 只有在控制器或方法上有审计特性时才记录详细信息
                if (controllerAuditAttr != null || methodAuditAttr != null)
                {
                    // 优先使用方法上的审计特性
                    var auditAttr = methodAuditAttr ?? controllerAuditAttr;
                    
                    auditLog.Description = auditAttr.Description;
                    auditLog.OperationType = auditAttr.OperationType.ToString();
                    
                    // 获取控制器上的DisplayName特性
                    var displayNameAttr = controllerType.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttr != null)
                    {
                        auditLog.DisplayName = displayNameAttr.DisplayName;
                    }
                    
                    // 从路由数据中提取实体ID
                    var entityIdParamName = auditAttr.EntityIdParamName;
                    if (context.Request.RouteValues.TryGetValue(entityIdParamName, out var entityId))
                    {
                        auditLog.EntityId = entityId?.ToString();
                    }
                    
                    // 设置实体名称
                    if (!string.IsNullOrEmpty(auditAttr.EntityName))
                    {
                        auditLog.EntityName = auditAttr.EntityName;
                    }
                    else
                    {
                        // 尝试从控制器名称推断实体名称
                        auditLog.EntityName = auditLog.ControllerName;
                        if (auditLog.EntityName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                        {
                            auditLog.EntityName = auditLog.EntityName.Substring(0, auditLog.EntityName.Length - 10);
                        }
                    }
                    
                    // 如果需要记录响应数据
                    if (auditAttr.LogResponseData)
                    {
                        responseBodyStream.Position = 0;
                        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                        auditLog.AfterData = SanitizeSensitiveData(responseBody);
                    }
                    
                    // 提取操作特性信息
                    var operationAttr = actionMethodInfo.GetCustomAttribute<OperationAttribute>();
                    if (operationAttr != null)
                    {
                        // 使用辅助类处理操作特性
                        var operationProperties = OperationAttributeHelper.ExtractOperationInfo(operationAttr);
                        foreach (var prop in operationProperties)
                        {
                            auditLog.AttributeProperties.Add(prop.Key, prop.Value);
                        }
                    }
                }
                else if (_options.EnableOperationTypeInference && controllerType != null && actionMethodInfo != null)
                {
                    // 自动推断审计信息
                    AutoInferAuditInformation(auditLog, context, controllerActionDescriptor);
                    
                    // 获取控制器上的DisplayName特性
                    var displayNameAttr = controllerType.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttr != null)
                    {
                        auditLog.DisplayName = displayNameAttr.DisplayName;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 记录处理过程中的错误，但不影响原始请求
            _logger.LogError(ex, "审计处理过程中发生错误");
            isSuccess = false;
            errorMessage = ex.Message;
        }
        finally
        {
            // 复制响应流到原始响应流
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
            
            // 停止计时
            stopwatch.Stop();
            auditLog.ExecutionDuration = stopwatch.ElapsedMilliseconds;
            auditLog.IsSuccess = isSuccess;
            auditLog.ErrorMessage = errorMessage;
            
            // 记录审计日志
            try
            {
                await auditService.LogAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录审计日志失败");
            }
        }
    }
    
    /// <summary>
    /// 从Endpoint提取控制器行为描述符
    /// </summary>
    private AuditControllerActionDescriptor ExtractControllerActionDescriptorFromEndpoint(Endpoint endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }
            
        // 检查是否为控制器端点
        var mvcDescriptor = endpoint.Metadata.GetMetadata<MvcControllerActionDescriptor>();
        if (mvcDescriptor != null)
        {
            // 转换为自定义AuditControllerActionDescriptor
            return new AuditControllerActionDescriptor
            {
                ControllerName = mvcDescriptor.ControllerName,
                ActionName = mvcDescriptor.ActionName,
                ControllerTypeInfo = mvcDescriptor.ControllerTypeInfo,
                MethodInfo = mvcDescriptor.MethodInfo
            };
        }
        
        // 如果不是标准的ControllerActionDescriptor（可能是最小API），则尝试提取信息
        var routeNameMetadata = endpoint.Metadata.GetMetadata<RouteNameMetadata>();
        var displayNameMetadata = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
        
        // 尝试从路由模板提取控制器和操作
        var routeEndpoint = endpoint as RouteEndpoint;
        if (routeEndpoint != null)
        {
            var routePattern = routeEndpoint.RoutePattern;
            if (routePattern.PathSegments.Count > 1)
            {
                // 尝试提取控制器和操作名称
                // 这里使用简单的规则，实际项目可能需要更复杂的逻辑
                string controllerName = null;
                string actionName = null;
                
                // 从路由值中提取控制器和操作名称
                var routeValues = routeEndpoint.RoutePattern.RequiredValues;
                
                if (routeValues.TryGetValue("controller", out var controllerValue))
                {
                    controllerName = controllerValue?.ToString();
                }
                
                if (routeValues.TryGetValue("action", out var actionValue))
                {
                    actionName = actionValue?.ToString();
                }
                
                if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                {
                    // 使用提取的控制器和操作名称获取详细信息
                    var controllerType = FindControllerType(controllerName);
                    if (controllerType != null)
                    {
                        var methodInfo = FindActionMethod(controllerType, actionName);
                        if (methodInfo != null)
                        {
                            return new AuditControllerActionDescriptor
                            {
                                ControllerName = controllerName,
                                ActionName = actionName,
                                ControllerTypeInfo = controllerType.GetTypeInfo(),
                                MethodInfo = methodInfo
                            };
                        }
                    }
                }
            }
        }
        
        return null;
    }

    /// <summary>
    /// 查找控制器类型
    /// </summary>
    private Type FindControllerType(string controllerName)
    {
        // 使用静态缓存提高性能
        return FindControllerTypeFromCache(controllerName);
    }
    
    // 静态缓存，存储控制器名称到类型的映射
    private static readonly ConcurrentDictionary<string, Type> _controllerTypeCache 
        = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// 从缓存中查找控制器类型
    /// </summary>
    private Type FindControllerTypeFromCache(string controllerName)
    {
        // 首先尝试从缓存获取
        if (_controllerTypeCache.TryGetValue(controllerName, out var cachedType))
        {
            return cachedType;
        }
        
        // 如果缓存中没有，则执行回退搜索
        return _controllerTypeCache.GetOrAdd(controllerName, name => 
        {
            // 搜索所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    // 检查是否为Web程序集或应用程序程序集
                    if (assembly.FullName.Contains("Microsoft") || 
                        assembly.FullName.Contains("System") || 
                        assembly.FullName.Contains("mscorlib"))
                    {
                        continue;
                    }
                    
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // 检查是否为controller
                        if (type.Name.Equals($"{name}Controller", StringComparison.OrdinalIgnoreCase) ||
                            type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("在程序集 {Assembly} 中发现控制器 {Controller}", assembly.GetName().Name, type.Name);
                            return type;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 忽略无法加载的程序集，但记录日志
                    _logger.LogTrace(ex, "在搜索控制器时跳过程序集 {Assembly}", assembly.GetName().Name);
                }
            }
            
            _logger.LogWarning("无法找到名为 {ControllerName} 的控制器", name);
            return null;
        });
    }
    
    /// <summary>
    /// 查找操作方法
    /// </summary>
    private MethodInfo FindActionMethod(Type controllerType, string actionName)
    {
        // 查找与操作名称匹配的公共方法
        return controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }
    
    /// <summary>
    /// 判断是否跳过审计
    /// </summary>
    private bool ShouldSkipAudit(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;
        
        // 检查是否为健康检查请求
        if (!_options.LogHealthChecks && requestPath.Contains("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 检查排除路径
        foreach (var excludedPrefix in _options.ExcludedPathPrefixes)
        {
            if (requestPath.StartsWith(excludedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取请求体
    /// </summary>
    private static async Task<string> GetRequestBodyAsync(HttpContext context)
    {
        // 启用重新读取请求体
        context.Request.EnableBuffering();
        
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        
        var requestBody = await reader.ReadToEndAsync();
        
        // 重置请求体位置，以便后续中间件可以读取
        context.Request.Body.Position = 0;
        
        return requestBody;
    }
    
    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // 尝试从多种代理头获取IP地址
        string[] headersToCheck = 
        { 
            "X-Forwarded-For",
            "X-Real-IP",
            "CF-Connecting-IP",  // Cloudflare
            "True-Client-IP",    // Akamai
            "X-Client-IP"
        };
        
        foreach (var header in headersToCheck)
        {
            if (context.Request.Headers.TryGetValue(header, out StringValues headerValue) && 
                !StringValues.IsNullOrEmpty(headerValue))
            {
                var value = headerValue.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // 如果是X-Forwarded-For，可能包含多个IP，取第一个
                    if (header.Equals("X-Forwarded-For", StringComparison.OrdinalIgnoreCase))
                    {
                        var ips = value.Split(',');
                        if (ips.Length > 0)
                        {
                            return ips[0].Trim();
                        }
                    }
                    return value.Trim();
                }
            }
        }
        
        // 尝试从 HTTP 连接特性获取IP地址
        var connection = context.Features.Get<IHttpConnectionFeature>();
        var remoteIp = connection?.RemoteIpAddress?.ToString();
        
        // 检查是否为本地回环地址
        if (string.IsNullOrEmpty(remoteIp) || remoteIp == "::1" || remoteIp == "127.0.0.1")
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "未知";
        }
        
        return remoteIp ?? "未知";
    }
    
    /// <summary>
    /// 获取用户代理
    /// </summary>
    private static string GetUserAgent(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            return userAgent.ToString();
        }
        
        return "未知";
    }
    
    /// <summary>
    /// 对敏感数据进行脱敏处理
    /// </summary>
    private string SanitizeSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data) || !_options.SensitiveData.Enabled)
        {
            return data;
        }
        
        try
        {
            // 尝试解析为JSON
            if (IsValidJson(data))
            {
                return SanitizeJson(data);
            }
            
            // 对查询字符串参数进行脱敏
            if (data.Contains('=') && data.Contains('&'))
            {
                return SanitizeQueryString(data);
            }
            
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "敏感数据脱敏处理失败");
            return data;
        }
    }
    
    /// <summary>
    /// 检查是否为有效的JSON
    /// </summary>
    private bool IsValidJson(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }
        
        input = input.Trim();
        if ((input.StartsWith("{") && input.EndsWith("}")) || (input.StartsWith("[") && input.EndsWith("]")))
        {
            try
            {
                JsonDocument.Parse(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理JSON中的敏感数据
    /// </summary>
    private string SanitizeJson(string json)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(json);
            var stream = new MemoryStream();
            
            using (var writer = new Utf8JsonWriter(stream))
            {
                SanitizeJsonElement(jsonDoc.RootElement, writer);
                writer.Flush();
                
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
        catch
        {
            return json;
        }
    }
    
    /// <summary>
    /// 脱敏JSON元素
    /// </summary>
    private void SanitizeJsonElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                
                foreach (var property in element.EnumerateObject())
                {
                    var propertyName = property.Name.ToLowerInvariant();
                    
                    // 检查是否为要排除的字段
                    if (_options.SensitiveData.ExcludedFields.Any(p => 
                        propertyName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        writer.WritePropertyName(property.Name);
                        writer.WriteStringValue("[已移除]");
                        continue;
                    }
                    
                    // 检查是否需要脱敏
                    bool isSensitive = _options.SensitiveData.SensitiveFieldPatterns.Any(p => 
                        propertyName.Contains(p, StringComparison.OrdinalIgnoreCase));
                    
                    writer.WritePropertyName(property.Name);
                    
                    if (isSensitive && property.Value.ValueKind == JsonValueKind.String)
                    {
                        writer.WriteStringValue(MaskSensitiveValue(property.Value.GetString()));
                    }
                    else
                    {
                        SanitizeJsonElement(property.Value, writer);
                    }
                }
                
                writer.WriteEndObject();
                break;
                
            case JsonValueKind.Array:
                writer.WriteStartArray();
                
                foreach (var item in element.EnumerateArray())
                {
                    SanitizeJsonElement(item, writer);
                }
                
                writer.WriteEndArray();
                break;
                
            default:
                WriteJsonValue(element, writer);
                break;
        }
    }
    
    /// <summary>
    /// 写入JSON值
    /// </summary>
    private void WriteJsonValue(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else
                {
                    writer.WriteNumberValue(element.GetDouble());
                }
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteStringValue(element.ToString());
                break;
        }
    }
    
    /// <summary>
    /// 脱敏查询字符串
    /// </summary>
    private string SanitizeQueryString(string queryString)
    {
        var resultParts = new List<string>();
        var parts = queryString.Split('&');
        
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part) || !part.Contains('='))
            {
                resultParts.Add(part);
                continue;
            }
            
            var keyValue = part.Split('=', 2);
            var key = keyValue[0];
            var value = keyValue.Length > 1 ? keyValue[1] : string.Empty;
            
            // 检查是否为要排除的字段
            if (_options.SensitiveData.ExcludedFields.Any(p => 
                key.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                resultParts.Add($"{key}=[已移除]");
                continue;
            }
            
            // 检查是否需要脱敏
            bool isSensitive = _options.SensitiveData.SensitiveFieldPatterns.Any(p => 
                key.Contains(p, StringComparison.OrdinalIgnoreCase));
            
            if (isSensitive)
            {
                resultParts.Add($"{key}={MaskSensitiveValue(value)}");
            }
            else
            {
                resultParts.Add(part);
            }
        }
        
        return string.Join("&", resultParts);
    }
    
    /// <summary>
    /// 掩码敏感值
    /// </summary>
    private string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        
        int keepFirstChars = _options.SensitiveData.KeepFirstChars;
        int keepLastChars = _options.SensitiveData.KeepLastChars;
        string maskChar = _options.SensitiveData.MaskCharacter;
        
        // 如果值太短，直接全部掩码
        if (value.Length <= keepFirstChars + keepLastChars)
        {
            return new string(maskChar[0], value.Length);
        }
        
        // 保留前几位和后几位，中间部分掩码
        StringBuilder result = new StringBuilder();
        
        // 保留前面的字符
        if (keepFirstChars > 0)
        {
            result.Append(value.Substring(0, keepFirstChars));
        }
        
        // 中间部分掩码
        int maskLength = value.Length - keepFirstChars - keepLastChars;
        result.Append(new string(maskChar[0], maskLength));
        
        // 保留后面的字符
        if (keepLastChars > 0)
        {
            result.Append(value.Substring(value.Length - keepLastChars));
        }
        
        return result.ToString();
    }

    /// <summary>
    /// 自动推断审计信息
    /// </summary>
    private void AutoInferAuditInformation(AuditLog auditLog, HttpContext context, AuditControllerActionDescriptor actionDescriptor)
    {
        try
        {
            // 推断操作类型
            auditLog.OperationType = InferOperationTypeFromHttpMethod(
                context.Request.Method,
                actionDescriptor.ActionName,
                _options.OperationInference);
            
            // 推断实体名称（如果尚未设置）
            if (string.IsNullOrEmpty(auditLog.EntityName))
            {
                auditLog.EntityName = auditLog.ControllerName;
                if (auditLog.EntityName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                {
                    auditLog.EntityName = auditLog.EntityName.Substring(0, auditLog.EntityName.Length - 10);
                }
            }
            
            // 尝试从路由数据中提取实体ID
            if (string.IsNullOrEmpty(auditLog.EntityId))
            {
                // 检查常见的ID参数名称
                foreach (var idParamName in _options.OperationInference.CommonIdParameterNames)
                {
                    string paramName = idParamName;
                    
                    // 替换{entityName}占位符
                    if (paramName.Contains("{entityName}"))
                    {
                        paramName = paramName.Replace("{entityName}", auditLog.EntityName);
                    }
                    
                    if (context.Request.RouteValues.TryGetValue(paramName, out var entityId) && entityId != null)
                    {
                        auditLog.EntityId = entityId.ToString();
                        break;
                    }
                }
            }
            
            // 生成描述信息
            if (string.IsNullOrEmpty(auditLog.Description))
            {
                auditLog.Description = GenerateDescription(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动推断审计信息时发生错误");
        }
    }
    
    /// <summary>
    /// 从HTTP方法推断操作类型
    /// </summary>
    private string InferOperationTypeFromHttpMethod(string httpMethod, string actionName, OperationInferenceOptions options)
    {
        // 首先检查方法名称中的关键词
        string methodNameLower = actionName.ToLowerInvariant();
        
        // 检查查询关键词
        foreach (var keyword in options.QueryKeywords)
        {
            if (methodNameLower.Contains(keyword.ToLowerInvariant()))
            {
                return AuditOperationType.Query.ToString();
            }
        }
        
        // 检查创建关键词
        foreach (var keyword in options.CreateKeywords)
        {
            if (methodNameLower.Contains(keyword.ToLowerInvariant()))
            {
                return AuditOperationType.Create.ToString();
            }
        }
        
        // 检查更新关键词
        foreach (var keyword in options.UpdateKeywords)
        {
            if (methodNameLower.Contains(keyword.ToLowerInvariant()))
            {
                return AuditOperationType.Update.ToString();
            }
        }
        
        // 检查删除关键词
        foreach (var keyword in options.DeleteKeywords)
        {
            if (methodNameLower.Contains(keyword.ToLowerInvariant()))
            {
                return AuditOperationType.Delete.ToString();
            }
        }
        
        // 如果方法名称中没有找到关键词，则使用HTTP方法映射
        if (options.HttpMethodMappings.TryGetValue(httpMethod, out var operationType))
        {
            return operationType;
        }
        
        // 默认返回Action
        return AuditOperationType.Action.ToString();
    }
    
    /// <summary>
    /// 根据审计日志生成描述信息
    /// </summary>
    private string GenerateDescription(AuditLog auditLog)
    {
        var entityName = string.IsNullOrEmpty(auditLog.EntityName) ? "数据" : auditLog.EntityName;
        
        switch (auditLog.OperationType)
        {
            case "Query":
                return $"查询{entityName}";
            case "Create":
                return $"创建{entityName}";
            case "Update":
                if (!string.IsNullOrEmpty(auditLog.EntityId))
                {
                    return $"更新{entityName}，ID: {auditLog.EntityId}";
                }
                return $"更新{entityName}";
            case "Delete":
                if (!string.IsNullOrEmpty(auditLog.EntityId))
                {
                    return $"删除{entityName}，ID: {auditLog.EntityId}";
                }
                return $"删除{entityName}";
            case "Login":
                return "用户登录";
            case "Logout":
                return "用户登出";
            case "Import":
                return $"导入{entityName}";
            case "Export":
                return $"导出{entityName}";
            case "Upload":
                return $"上传{entityName}";
            case "Download":
                return $"下载{entityName}";
            case "Authorize":
                return $"授权{entityName}";
            case "Setting":
                return $"设置{entityName}";
            case "Batch":
                return $"批量处理{entityName}";
            default:
                return $"操作{entityName}";
        }
    }
} 