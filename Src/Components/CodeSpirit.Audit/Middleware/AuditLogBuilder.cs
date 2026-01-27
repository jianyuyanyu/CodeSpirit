using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Models;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using MvcControllerActionDescriptor = Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Audit.Middleware;

/// <summary>
/// 审计日志构建器
/// </summary>
/// <remarks>
/// 专门负责构建审计日志对象
/// </remarks>
public class AuditLogBuilder
{
    private readonly AuditOptions _options;
    private readonly ILogger<AuditLogBuilder> _logger;
    private readonly ControllerTypeRegistry _controllerTypeRegistry;
    private readonly SensitiveDataProcessor _sensitiveDataProcessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogBuilder(
        IOptions<AuditOptions> options,
        ILogger<AuditLogBuilder> logger,
        ControllerTypeRegistry controllerTypeRegistry,
        SensitiveDataProcessor sensitiveDataProcessor)
    {
        _options = options.Value;
        _logger = logger;
        _controllerTypeRegistry = controllerTypeRegistry;
        _sensitiveDataProcessor = sensitiveDataProcessor;
    }

    /// <summary>
    /// 构建审计日志
    /// </summary>
    /// <param name="context">审计上下文</param>
    /// <param name="httpContext">HTTP上下文</param>
    /// <param name="originalRequestBody">原始请求体</param>
    /// <param name="responseBodyStream">响应体流</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="statusCode">状态码</param>
    /// <param name="executionDuration">执行时长（毫秒）</param>
    /// <returns>审计日志和是否跳过审计的标志</returns>
    public async Task<(Models.AuditLog AuditLog, bool ShouldSkipAudit)> BuildAsync(
        AuditContext context,
        HttpContext httpContext,
        string originalRequestBody,
        MemoryStream responseBodyStream,
        bool isSuccess,
        string errorMessage,
        int statusCode,
        long executionDuration)
    {
        var auditLog = new Models.AuditLog
        {
            TenantId = context.TenantId,
            RequestPath = context.RequestPath,
            RequestMethod = context.RequestMethod,
            IpAddress = context.IpAddress,
            UserAgent = context.UserAgent,
            UserId = context.UserId,
            UserName = context.UserName,
            RequestParams = _options.LogRequestParams ? _sensitiveDataProcessor.Sanitize(originalRequestBody) : null,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
            ExecutionDuration = executionDuration,
            OperationTime = DateTime.UtcNow
        };

        // 提取控制器和方法信息
        var controllerActionDescriptor = ExtractControllerActionDescriptor(httpContext);
        bool shouldSkipAudit = false;
        if (controllerActionDescriptor != null)
        {
            shouldSkipAudit = await EnrichAuditLogFromController(auditLog, controllerActionDescriptor, httpContext, responseBodyStream);
        }

        // 从响应头获取审计元数据（分布式场景）
        await EnrichAuditLogFromHeaders(auditLog, httpContext, responseBodyStream);

        return (auditLog, shouldSkipAudit);
    }

    /// <summary>
    /// 提取控制器操作描述符
    /// </summary>
    private AuditControllerActionDescriptor? ExtractControllerActionDescriptor(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            // 尝试从Endpoint获取
            var mvcDescriptor = endpoint.Metadata.GetMetadata<MvcControllerActionDescriptor>();
            if (mvcDescriptor != null)
            {
                return new AuditControllerActionDescriptor
                {
                    ControllerName = mvcDescriptor.ControllerName,
                    ActionName = mvcDescriptor.ActionName,
                    ControllerTypeInfo = mvcDescriptor.ControllerTypeInfo,
                    MethodInfo = mvcDescriptor.MethodInfo
                };
            }
        }

        // 从路由数据提取
        var routeData = context.GetRouteData();
        if (routeData != null &&
            routeData.Values.TryGetValue("controller", out var controllerName) &&
            routeData.Values.TryGetValue("action", out var actionName))
        {
            var controllerStr = controllerName?.ToString();
            var actionStr = actionName?.ToString();

            if (!string.IsNullOrEmpty(controllerStr) && !string.IsNullOrEmpty(actionStr))
            {
                var controllerType = _controllerTypeRegistry.FindControllerType(controllerStr);
                if (controllerType != null)
                {
                    var methodInfo = FindActionMethod(controllerType, actionStr);
                    if (methodInfo != null)
                    {
                        return new AuditControllerActionDescriptor
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

        return null;
    }

    /// <summary>
    /// 从控制器信息丰富审计日志
    /// </summary>
    /// <returns>是否应该跳过审计</returns>
    private async Task<bool> EnrichAuditLogFromController(
        Models.AuditLog auditLog,
        AuditControllerActionDescriptor controllerActionDescriptor,
        HttpContext context,
        MemoryStream responseBodyStream)
    {
        var controllerType = controllerActionDescriptor.ControllerTypeInfo;
        var actionMethodInfo = controllerActionDescriptor.MethodInfo;

        // 检查NoAudit特性
        var methodNoAuditAttr = actionMethodInfo.GetCustomAttribute<NoAuditAttribute>();
        var controllerNoAuditAttr = controllerType.GetCustomAttribute<NoAuditAttribute>();
        if (methodNoAuditAttr != null || controllerNoAuditAttr != null)
        {
            return true; // 标记为跳过审计
        }

        // 获取审计特性
        var controllerAuditAttr = controllerType.GetCustomAttribute<AuditAttribute>();
        var methodAuditAttr = actionMethodInfo.GetCustomAttribute<AuditAttribute>();

        if (controllerAuditAttr != null || methodAuditAttr != null)
        {
            var auditAttr = methodAuditAttr ?? controllerAuditAttr;
            auditLog.Description = auditAttr.Description;
            auditLog.OperationName = auditAttr.Description;
            auditLog.OperationType = auditAttr.OperationType.ToString();

            // 如果需要记录响应数据
            if (auditAttr.LogResponseData)
            {
                responseBodyStream.Position = 0;
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                auditLog.AfterData = _sensitiveDataProcessor.Sanitize(responseBody);
            }

            // 提取操作特性信息
            var operationAttr = actionMethodInfo.GetCustomAttribute<OperationAttribute>();
            if (operationAttr != null)
            {
                var operationProperties = OperationAttributeHelper.ExtractOperationInfo(operationAttr);
                foreach (var prop in operationProperties)
                {
                    auditLog.AttributeProperties.Add(prop.Key, prop.Value);
                }
            }
        }
        else if (_options.EnableOperationTypeInference)
        {
            // 自动推断审计信息
            AutoInferAuditInformation(auditLog, controllerActionDescriptor);
        }

        return false; // 不跳过审计
    }

    /// <summary>
    /// 从响应头丰富审计日志
    /// </summary>
    private async Task EnrichAuditLogFromHeaders(
        Models.AuditLog auditLog,
        HttpContext context,
        MemoryStream responseBodyStream)
    {
        // 优先使用新的 JSON 响应头格式
        AuditMetadata? metadata = null;

        if (context.Response.Headers.TryGetValue("X-Audit-Metadata", out var metadataHeader))
        {
            try
            {
                var base64Value = metadataHeader.ToString();
                if (!string.IsNullOrEmpty(base64Value))
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
                    metadata = JsonConvert.DeserializeObject<AuditMetadata>(json);

                    if (metadata != null)
                    {
                        ApplyMetadataToAuditLog(auditLog, metadata, responseBodyStream);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析JSON审计元数据失败，回退到旧格式");
            }
        }

        // 向后兼容：解析旧的多个响应头
        //ParseLegacyHeaders(auditLog, context, responseBodyStream);
    }

    /// <summary>
    /// 应用元数据到审计日志
    /// </summary>
    private async Task ApplyMetadataToAuditLog(
        Models.AuditLog auditLog,
        AuditMetadata metadata,
        MemoryStream responseBodyStream)
    {
        if (!string.IsNullOrEmpty(metadata.OperationName))
            auditLog.OperationName = metadata.OperationName;

        if (!string.IsNullOrEmpty(metadata.OperationType))
            auditLog.OperationType = metadata.OperationType;

        if (!string.IsNullOrEmpty(metadata.Controller))
            auditLog.AdditionalData["ApiController"] = metadata.Controller;

        if (!string.IsNullOrEmpty(metadata.Action))
            auditLog.AdditionalData["ApiAction"] = metadata.Action;

        if (!string.IsNullOrEmpty(metadata.Description) && string.IsNullOrEmpty(auditLog.Description))
            auditLog.Description = metadata.Description;

        if (!metadata.LogRequestParams)
            auditLog.RequestParams = null;

        if (metadata.LogResponseData && string.IsNullOrEmpty(auditLog.AfterData))
        {
            responseBodyStream.Position = 0;
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            auditLog.AfterData = _sensitiveDataProcessor.Sanitize(responseBody);
        }

        if (!string.IsNullOrEmpty(metadata.EntityName))
            auditLog.AdditionalData["EntityName"] = metadata.EntityName;

        if (!string.IsNullOrEmpty(metadata.EntityIdParamName))
            auditLog.AdditionalData["EntityIdParamName"] = metadata.EntityIdParamName;

        if (!string.IsNullOrEmpty(metadata.OperationLabel))
            auditLog.AttributeProperties["OperationLabel"] = metadata.OperationLabel;

        if (!string.IsNullOrEmpty(metadata.OperationActionType))
            auditLog.AttributeProperties["OperationActionType"] = metadata.OperationActionType;

        if (!string.IsNullOrEmpty(metadata.OperationApi))
            auditLog.AttributeProperties["OperationApi"] = metadata.OperationApi;

        if (!string.IsNullOrEmpty(metadata.OperationConfirmText))
            auditLog.AttributeProperties["OperationConfirmText"] = metadata.OperationConfirmText;

        if (!string.IsNullOrEmpty(metadata.OperationIcon))
            auditLog.AttributeProperties["OperationIcon"] = metadata.OperationIcon;

        if (metadata.IsBulkOperation)
            auditLog.AttributeProperties["IsBulkOperation"] = "true";
    }

    /// <summary>
    /// 解析旧的响应头格式（向后兼容）
    /// </summary>
    private async Task ParseLegacyHeaders(
        Models.AuditLog auditLog,
        HttpContext context,
        MemoryStream responseBodyStream)
    {
        if (context.Response.Headers.TryGetValue("X-Audit-OperationName", out var operationNameHeader))
        {
            var headerValue = operationNameHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                auditLog.OperationName = DecodeHeaderValue(headerValue);
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-OperationType", out var operationTypeHeader))
        {
            var headerValue = operationTypeHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                auditLog.OperationType = DecodeHeaderValue(headerValue);
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-Controller", out var controllerHeader))
        {
            var headerValue = controllerHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                auditLog.AdditionalData["ApiController"] = DecodeHeaderValue(headerValue);
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-Action", out var actionHeader))
        {
            var headerValue = actionHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue))
            {
                auditLog.AdditionalData["ApiAction"] = DecodeHeaderValue(headerValue);
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-Description", out var descriptionHeader))
        {
            var headerValue = descriptionHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue) && string.IsNullOrEmpty(auditLog.Description))
            {
                auditLog.Description = DecodeHeaderValue(headerValue);
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-LogRequestParams", out var logRequestParamsHeader))
        {
            var headerValue = logRequestParamsHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue) && bool.TryParse(headerValue, out var logRequestParams))
            {
                if (!logRequestParams)
                    auditLog.RequestParams = null;
            }
        }

        if (context.Response.Headers.TryGetValue("X-Audit-LogResponseData", out var logResponseDataHeader))
        {
            var headerValue = logResponseDataHeader.ToString();
            if (!string.IsNullOrEmpty(headerValue) && bool.TryParse(headerValue, out var logResponseData))
            {
                if (logResponseData && string.IsNullOrEmpty(auditLog.AfterData))
                {
                    responseBodyStream.Position = 0;
                    var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                    auditLog.AfterData = _sensitiveDataProcessor.Sanitize(responseBody);
                }
            }
        }
    }

    /// <summary>
    /// 安全地解码响应头值
    /// </summary>
    private static string DecodeHeaderValue(string encodedValue)
    {
        if (string.IsNullOrEmpty(encodedValue))
            return encodedValue;

        try
        {
            return HttpUtility.UrlDecode(encodedValue);
        }
        catch
        {
            return encodedValue;
        }
    }

    /// <summary>
    /// 查找操作方法
    /// </summary>
    private MethodInfo? FindActionMethod(Type controllerType, string actionName)
    {
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        return methods.FirstOrDefault(m =>
            m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase) &&
            !m.IsSpecialName &&
            m.GetCustomAttribute<NonActionAttribute>() == null);
    }

    /// <summary>
    /// 自动推断审计信息
    /// </summary>
    private void AutoInferAuditInformation(
        Models.AuditLog auditLog,
        AuditControllerActionDescriptor controllerActionDescriptor)
    {
        var controllerType = controllerActionDescriptor.ControllerTypeInfo;
        var displayNameAttr = controllerType.GetCustomAttribute<DisplayNameAttribute>();
        if (displayNameAttr != null)
        {
            auditLog.OperationName = displayNameAttr.DisplayName;
        }
        else
        {
            auditLog.OperationName = controllerActionDescriptor.ControllerName;
        }

        // 推断操作类型
        if (string.IsNullOrEmpty(auditLog.OperationType))
        {
            auditLog.OperationType = InferOperationTypeFromHttpMethod(
                auditLog.RequestMethod,
                controllerActionDescriptor.ActionName,
                _options.OperationInference);
        }

        // 生成描述信息
        if (string.IsNullOrEmpty(auditLog.Description))
        {
            auditLog.Description = GenerateDescription(auditLog);
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
    private string GenerateDescription(Models.AuditLog auditLog)
    {
        switch (auditLog.OperationType)
        {
            case "Query":
                return "查询数据";
            case "Create":
                return "创建数据";
            case "Update":
                return "更新数据";
            case "Delete":
                return "删除数据";
            case "Login":
                return "用户登录";
            case "Logout":
                return "用户登出";
            case "Import":
                return "导入数据";
            case "Export":
                return "导出数据";
            case "Upload":
                return "上传数据";
            case "Download":
                return "下载数据";
            case "Authorize":
                return "授权操作";
            case "Setting":
                return "设置操作";
            case "Batch":
                return "批量处理";
            default:
                return "数据操作";
        }
    }
}
