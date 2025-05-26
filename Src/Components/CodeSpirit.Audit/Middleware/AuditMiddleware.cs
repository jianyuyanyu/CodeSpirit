using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MvcControllerActionDescriptor = Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 审计中间件
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly AuditOptions _options;

    // 使用弱引用缓存避免内存泄漏，并添加定期清理机制
    private static readonly ConcurrentDictionary<string, WeakReference<Type>> _controllerTypeCache
        = new ConcurrentDictionary<string, WeakReference<Type>>(StringComparer.OrdinalIgnoreCase);
    
    private static readonly Timer _cacheCleanupTimer = new Timer(CleanupCache, null, 
        TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditMiddleware(
        RequestDelegate next,
        ILogger<AuditMiddleware> logger,
        IConfiguration configuration,
        IActionDescriptorCollectionProvider? actionDescriptorCollectionProvider = null)
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
    /// 清理缓存中的无效弱引用
    /// </summary>
    private static void CleanupCache(object? state)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _controllerTypeCache)
        {
            if (!kvp.Value.TryGetTarget(out _))
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _controllerTypeCache.TryRemove(key, out _);
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

                    _controllerTypeCache.TryAdd(controllerName, new WeakReference<Type>(controllerType));
                }
            }

            _logger.LogInformation("已从应用程序部件初始化控制器类型缓存，共 {Count} 个控制器", _controllerTypeCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化控制器类型缓存失败");
        }
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService, IClientIpService clientIpService)
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

        // 保存原始请求体
        var originalRequestBody = await GetRequestBodyAsync(context);

        // 记录响应
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // 获取客户端IP地址
        var ipAddress = clientIpService.GetClientIpAddress(context);

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

            // 获取控制器和方法信息
            var endpoint = context.GetEndpoint();
            AuditControllerActionDescriptor controllerActionDescriptor = null;

            _logger.LogInformation("开始提取控制器和操作信息 - Endpoint: {Endpoint}", endpoint?.DisplayName ?? "null");

            if (endpoint != null)
            {
                // 尝试从Endpoint获取控制器和操作信息
                controllerActionDescriptor = ExtractControllerActionDescriptorFromEndpoint(endpoint);

                _logger.LogInformation("从Endpoint提取控制器和操作信息 - 结果: {Result}",
                    controllerActionDescriptor != null ? "成功" : "失败");

                // 如果无法从Endpoint获取，则退回到路由数据方法
                if (controllerActionDescriptor == null)
                {
                    // 从路由数据中提取控制器和操作信息
                    var routeData = context.GetRouteData();
                    _logger.LogInformation("尝试从路由数据提取 - 路由数据: {RouteData}",
                        routeData != null ? "存在" : "不存在");

                    if (routeData != null &&
                        routeData.Values.TryGetValue("controller", out var controllerName) &&
                        routeData.Values.TryGetValue("action", out var actionName))
                    {
                        var controllerStr = controllerName?.ToString();
                        var actionStr = actionName?.ToString();

                        _logger.LogInformation("从路由数据提取到控制器和操作 - 控制器: {Controller}, 操作: {Action}",
                            controllerStr, actionStr);

                        if (!string.IsNullOrEmpty(controllerStr) && !string.IsNullOrEmpty(actionStr))
                        {
                            // 尝试获取控制器类型
                            var controllerType = FindControllerType(controllerStr);
                            _logger.LogInformation("查找控制器类型 - 结果: {Result}",
                                controllerType != null ? "成功" : "失败");

                            if (controllerType != null)
                            {
                                // 尝试获取操作方法
                                var methodInfo = FindActionMethod(controllerType, actionStr);
                                _logger.LogInformation("查找操作方法 - 结果: {Result}",
                                    methodInfo != null ? "成功" : "失败");

                                if (methodInfo != null)
                                {
                                    controllerActionDescriptor = new AuditControllerActionDescriptor
                                    {
                                        ControllerName = controllerStr,
                                        ActionName = actionStr,
                                        ControllerTypeInfo = controllerType.GetTypeInfo(),
                                        MethodInfo = methodInfo
                                    };

                                    _logger.LogInformation("成功创建控制器操作描述符");
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("控制器操作描述符提取结果: {Result}",
                controllerActionDescriptor != null ?
                    $"成功 - 控制器: {controllerActionDescriptor.ControllerName}, 操作: {controllerActionDescriptor.ActionName}" :
                    "失败 - 未能提取控制器和操作信息");

            // 提取控制器和方法信息
            if (controllerActionDescriptor != null)
            {
                var controllerType = controllerActionDescriptor.ControllerTypeInfo;
                var actionMethodInfo = controllerActionDescriptor.MethodInfo;

                _logger.LogInformation("提取到控制器和方法信息 - 控制器: {Controller}, 操作: {Action}",
                    controllerActionDescriptor.ControllerName,
                    controllerActionDescriptor.ActionName);

                auditLog.StatusCode = context.Response.StatusCode;
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

                    _logger.LogInformation("找到审计特性 - 描述: {Description}, 操作类型: {OperationType}",
                        auditAttr.Description, auditAttr.OperationType);

                    // 设置审计信息
                    auditLog.Description = auditAttr.Description;
                    auditLog.OperationName = auditAttr.Description;

                    // 保存原始的控制器和操作名称用于调试
                    auditLog.AdditionalData["OriginalController"] = controllerActionDescriptor.ControllerName;
                    auditLog.AdditionalData["OriginalAction"] = controllerActionDescriptor.ActionName;

                    // 确保正确设置操作类型 - 修复操作类型设置问题
                    var operationType = auditAttr.OperationType;
                    string operationTypeString = operationType.ToString();
                    _logger.LogInformation("设置操作类型 - 控制器: {Controller}, 操作: {Action}, 枚举值: {EnumValue}, 字符串值: {StringValue}",
                        controllerActionDescriptor.ControllerName,
                        controllerActionDescriptor.ActionName,
                        (int)operationType, operationTypeString);

                    // 直接设置操作类型字符串
                    auditLog.OperationType = operationTypeString;

                    _logger.LogDebug("从特性获取操作类型: {OperationType}, 枚举值: {EnumValue}, 转换后: {ConvertedValue}, 设置后的值: {SetValue}",
                        operationType, (int)operationType, operationTypeString, auditLog.OperationType);

                    // 强制确保OperationType不为空
                    if (string.IsNullOrEmpty(auditLog.OperationType))
                    {
                        _logger.LogWarning("操作类型字符串转换后为空，强制使用枚举值名称");
                        auditLog.OperationType = Enum.GetName(typeof(AuditOperationType), operationType) ?? "Action";
                    }

                    // 记录OperationName和Description以便调试
                    _logger.LogInformation("设置审计信息 - Description: {Description}, OperationName: {OperationName}",
                        auditLog.Description, auditLog.OperationName);

                    // 获取控制器上的DisplayName特性
                    var displayNameAttr = controllerType.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAttr != null)
                    {
                        auditLog.OperationName = displayNameAttr.DisplayName;
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
                        auditLog.OperationName = displayNameAttr.DisplayName;
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
            auditLog.StatusCode = context.Response.StatusCode;

            // 记录审计日志
            try
            {
                await auditService.LogAsync(auditLog);
                
                // 标记请求已被审计处理（用于性能监控）
                context.Items["AuditProcessed"] = true;
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "记录审计日志失败");
            }
        }
    }

    /// <summary>
    /// 从Endpoint提取控制器和操作信息
    /// </summary>
    private AuditControllerActionDescriptor ExtractControllerActionDescriptorFromEndpoint(Endpoint endpoint)
    {
        if (endpoint == null)
        {
            _logger.LogInformation("Endpoint为null，无法提取控制器和操作信息");
            return null;
        }

        // 检查是否为控制器端点
        var mvcDescriptor = endpoint.Metadata.GetMetadata<MvcControllerActionDescriptor>();
        _logger.LogInformation("尝试获取MvcControllerActionDescriptor - 结果: {Result}",
            mvcDescriptor != null ? "成功" : "失败");

        if (mvcDescriptor != null)
        {
            _logger.LogInformation("从MvcControllerActionDescriptor获取信息 - 控制器: {Controller}, 操作: {Action}",
                mvcDescriptor.ControllerName, mvcDescriptor.ActionName);

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

        _logger.LogInformation("尝试获取其他元数据 - RouteNameMetadata: {RouteNameMetadata}, EndpointNameMetadata: {EndpointNameMetadata}",
            routeNameMetadata != null ? "存在" : "不存在",
            displayNameMetadata != null ? "存在" : "不存在");

        // 尝试从路由模板提取控制器和操作
        var routeEndpoint = endpoint as RouteEndpoint;
        _logger.LogInformation("检查是否为RouteEndpoint - 结果: {Result}",
            routeEndpoint != null ? "是" : "否");

        if (routeEndpoint != null)
        {
            var routePattern = routeEndpoint.RoutePattern;
            _logger.LogInformation("路由模式 - 原始文本: {RawText}, 路径段数量: {SegmentCount}",
                routePattern.RawText, routePattern.PathSegments.Count);

            if (routePattern.PathSegments.Count > 1)
            {
                // 尝试提取控制器和操作名称
                // 这里使用简单的规则，实际项目可能需要更复杂的逻辑
                string controllerName = null;
                string actionName = null;

                // 从路由值中提取控制器和操作名称
                var routeValues = routePattern.RequiredValues;
                _logger.LogInformation("路由必需值数量: {Count}", routeValues.Count);

                foreach (var kvp in routeValues)
                {
                    _logger.LogInformation("路由值 - 键: {Key}, 值: {Value}", kvp.Key, kvp.Value);
                }

                if (routeValues.TryGetValue("controller", out var controllerValue))
                {
                    controllerName = controllerValue?.ToString();
                    _logger.LogInformation("从路由值获取控制器名称: {Controller}", controllerName);
                }

                if (routeValues.TryGetValue("action", out var actionValue))
                {
                    actionName = actionValue?.ToString();
                    _logger.LogInformation("从路由值获取操作名称: {Action}", actionName);
                }

                if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                {
                    // 使用提取的控制器和操作名称获取详细信息
                    var controllerType = FindControllerType(controllerName);
                    _logger.LogInformation("查找控制器类型 - 结果: {Result}",
                        controllerType != null ? "成功" : "失败");

                    if (controllerType != null)
                    {
                        var methodInfo = FindActionMethod(controllerType, actionName);
                        _logger.LogInformation("查找操作方法 - 结果: {Result}",
                            methodInfo != null ? "成功" : "失败");

                        if (methodInfo != null)
                        {
                            _logger.LogInformation("成功创建AuditControllerActionDescriptor");
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

        _logger.LogInformation("未能从Endpoint提取控制器和操作信息");
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

    /// <summary>
    /// 从缓存中查找控制器类型
    /// </summary>
    private Type? FindControllerTypeFromCache(string controllerName)
    {
        // 首先尝试从缓存获取
        if (_controllerTypeCache.TryGetValue(controllerName, out var weakRef) && 
            weakRef.TryGetTarget(out var cachedType))
        {
            return cachedType;
        }

        // 如果缓存中没有，则执行优化的搜索
        return FindAndCacheControllerType(controllerName);
    }

    /// <summary>
    /// 查找并缓存控制器类型（优化版本）
    /// </summary>
    private Type? FindAndCacheControllerType(string controllerName)
    {
        // 优先搜索应用程序程序集
        var appAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.FullName?.StartsWith("Microsoft.") == true &&
                       !a.FullName?.StartsWith("System.") == true &&
                       !a.FullName?.StartsWith("mscorlib") == true &&
                       !a.IsDynamic)
            .ToList();

        foreach (var assembly in appAssemblies)
        {
            try
            {
                // 使用更高效的类型查找
                var controllerType = assembly.GetTypes()
                    .FirstOrDefault(type => 
                        (type.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase) ||
                         type.Name.Equals(controllerName, StringComparison.OrdinalIgnoreCase)) &&
                        type.IsClass && !type.IsAbstract);

                if (controllerType != null)
                {
                    _controllerTypeCache.TryAdd(controllerName, new WeakReference<Type>(controllerType));
                    _logger.LogDebug("在程序集 {Assembly} 中发现控制器 {Controller}", 
                        assembly.GetName().Name, controllerType.Name);
                    return controllerType;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogTrace(ex, "在搜索控制器时跳过程序集 {Assembly}，部分类型无法加载", 
                    assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "在搜索控制器时跳过程序集 {Assembly}", assembly.GetName().Name);
            }
        }

        _logger.LogWarning("无法找到名为 {ControllerName} 的控制器", controllerName);
        return null;
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
    /// 检查是否需要跳过审计
    /// </summary>
    private bool ShouldSkipAudit(HttpContext context)
    {
        // 检查请求路径是否在排除列表中
        var requestPath = context.Request.Path.Value;

        // 检查是否为静态文件或其他需要排除的路径
        foreach (var excludePath in _options.ExcludedPathPrefixes)
        {
            if (requestPath.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("跳过审计 - 路径在排除列表中: {Path}", requestPath);
                return true;
            }
        }

        // 检查是否为健康检查或其他系统路径
        if (requestPath.Contains("/health", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Contains("/metrics", StringComparison.OrdinalIgnoreCase) ||
            requestPath.Contains("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("跳过审计 - 系统路径: {Path}", requestPath);
            return true;
        }

        // 检查是否为NoAudit控制器
        if (requestPath.Contains("/NoAudit", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("跳过审计 - NoAudit控制器: {Path}", requestPath);
            return true;
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
    /// 检查是否为有效的JSON并返回解析结果
    /// </summary>
    private (bool IsValid, JsonDocument? Document) TryParseJson(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (false, null);
        }

        input = input.Trim();
        if (!((input.StartsWith("{") && input.EndsWith("}")) || 
              (input.StartsWith("[") && input.EndsWith("]"))))
        {
            return (false, null);
        }

        try
        {
            var document = JsonDocument.Parse(input);
            return (true, document);
        }
        catch (JsonException)
        {
            return (false, null);
        }
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
            var (isValidJson, jsonDoc) = TryParseJson(data);
            if (isValidJson && jsonDoc != null)
            {
                using (jsonDoc) // 确保JsonDocument被正确释放
                {
                    return SanitizeJson(jsonDoc);
                }
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
    /// 处理JSON中的敏感数据
    /// </summary>
    private string SanitizeJson(JsonDocument jsonDoc)
    {
        try
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                SanitizeJsonElement(jsonDoc.RootElement, writer);
                writer.Flush();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JSON脱敏处理失败");
            return "[JSON脱敏失败]";
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
            // 推断操作类型 - 只有在尚未设置操作类型时才进行推断
            if (string.IsNullOrEmpty(auditLog.OperationType))
            {
                auditLog.OperationType = InferOperationTypeFromHttpMethod(
                    context.Request.Method,
                    actionDescriptor.ActionName,
                    _options.OperationInference);
            }

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