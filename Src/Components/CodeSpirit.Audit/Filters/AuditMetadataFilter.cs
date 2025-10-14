using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel;
using System.Reflection;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Web;
using MvcControllerActionDescriptor = Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;

namespace CodeSpirit.Audit.Filters;

/// <summary>
/// 审计元数据过滤器 - 将审计信息添加到响应头
/// 用于分布式环境中，让Web项目能够获取API服务的审计元数据
/// </summary>
public class AuditMetadataFilter : IActionFilter
{
    private readonly ILogger<AuditMetadataFilter> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AuditMetadataFilter(ILogger<AuditMetadataFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 安全地编码响应头值，处理中文字符
    /// </summary>
    /// <param name="value">原始值</param>
    /// <returns>编码后的值</returns>
    private static string EncodeHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // 使用URL编码处理中文字符
        return HttpUtility.UrlEncode(value);
    }

    /// <summary>
    /// 动作执行前
    /// </summary>
    /// <param name="context">动作执行上下文</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 执行前不需要处理
    }

    /// <summary>
    /// 动作执行后 - 将审计元数据添加到响应头
    /// </summary>
    /// <param name="context">动作执行上下文</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        try
        {
            var controllerType = context.Controller.GetType();
            var actionDescriptor = context.ActionDescriptor as MvcControllerActionDescriptor;
            
            if (actionDescriptor == null)
            {
                _logger.LogDebug("无法获取ControllerActionDescriptor，跳过审计元数据添加");
                return;
            }

            var methodInfo = actionDescriptor.MethodInfo;

            // 检查是否有NoAudit特性（方法级别优先于控制器级别）
            var methodNoAuditAttr = methodInfo.GetCustomAttribute<NoAuditAttribute>();
            var controllerNoAuditAttr = controllerType.GetCustomAttribute<NoAuditAttribute>();

            if (methodNoAuditAttr != null || controllerNoAuditAttr != null)
            {
                var noAuditAttr = methodNoAuditAttr ?? controllerNoAuditAttr;
                var reason = !string.IsNullOrEmpty(noAuditAttr.Reason) ? $" - 原因: {noAuditAttr.Reason}" : "";
                
                _logger.LogDebug("跳过审计元数据添加 - 控制器或方法标记了NoAudit特性: {Controller}.{Action}{Reason}",
                    actionDescriptor.ControllerName,
                    actionDescriptor.ActionName,
                    reason);
                
                return;
            }

            // 获取控制器的DisplayName
            var controllerDisplayName = controllerType
                .GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
                ?? actionDescriptor.ControllerName;

            // 获取方法的DisplayName
            var methodDisplayName = methodInfo
                .GetCustomAttribute<DisplayNameAttribute>()?.DisplayName 
                ?? actionDescriptor.ActionName;

            _logger.LogDebug("获取到控制器显示名称: {ControllerDisplayName}, 方法显示名称: {MethodDisplayName}", 
                controllerDisplayName, methodDisplayName);

            // 获取审计特性（优先方法级别，然后控制器级别）
            var auditAttr = methodInfo.GetCustomAttribute<AuditAttribute>() 
                ?? controllerType.GetCustomAttribute<AuditAttribute>();

            // 获取操作特性
            var operationAttr = methodInfo.GetCustomAttribute<OperationAttribute>();

            string operationName = controllerDisplayName;
            string operationType = "Action";
            string description = string.Empty;

            if (auditAttr != null)
            {
                // 如果有审计特性，使用特性中的信息
                operationName = !string.IsNullOrEmpty(auditAttr.Description) 
                    ? auditAttr.Description 
                    : controllerDisplayName;
                operationType = auditAttr.OperationType.ToString();
                description = auditAttr.Description;
            }
            else
            {
                // 如果没有审计特性，尝试组合控制器和方法的DisplayName
                if (!string.IsNullOrEmpty(methodDisplayName) && methodDisplayName != actionDescriptor.ActionName)
                {
                    operationName = $"{controllerDisplayName}-{methodDisplayName}";
                }
            }

            // 添加到响应头（只在响应头尚未设置时添加）
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationName"))
            {
                context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationName", EncodeHeaderValue(operationName));
            }
            
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationType"))
            {
                context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationType", EncodeHeaderValue(operationType));
            }
            
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-Controller"))
            {
                context.HttpContext.Response.Headers.TryAdd("X-Audit-Controller", EncodeHeaderValue(controllerDisplayName));
            }
            
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-Action"))
            {
                context.HttpContext.Response.Headers.TryAdd("X-Audit-Action", EncodeHeaderValue(methodDisplayName));
            }

            // 如果有描述信息，也添加到响应头
            if (!string.IsNullOrEmpty(description) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-Description"))
            {
                context.HttpContext.Response.Headers.TryAdd("X-Audit-Description", EncodeHeaderValue(description));
            }

            // 添加审计特性的额外字段
            if (auditAttr != null)
            {
                // 传递审计配置信息
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-LogRequestParams"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-LogRequestParams", auditAttr.LogRequestParams.ToString().ToLower());
                }
                
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-LogResponseData"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-LogResponseData", auditAttr.LogResponseData.ToString().ToLower());
                }
                
                if (!string.IsNullOrEmpty(auditAttr.EntityName) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-EntityName"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-EntityName", EncodeHeaderValue(auditAttr.EntityName));
                    _logger.LogDebug("添加审计实体名称到响应头: 实体={EntityName}", auditAttr.EntityName);
                }
                
                if (!string.IsNullOrEmpty(auditAttr.EntityIdParamName) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-EntityIdParamName"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-EntityIdParamName", EncodeHeaderValue(auditAttr.EntityIdParamName));
                }
            }

            // 添加操作特性信息
            if (operationAttr != null)
            {
                if (!string.IsNullOrEmpty(operationAttr.Label) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationLabel"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationLabel", EncodeHeaderValue(operationAttr.Label));
                }
                
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationActionType"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationActionType", EncodeHeaderValue(operationAttr.ActionType));
                }
                
                if (!string.IsNullOrEmpty(operationAttr.Api) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationApi"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationApi", EncodeHeaderValue(operationAttr.Api));
                }
                
                if (!string.IsNullOrEmpty(operationAttr.ConfirmText) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationConfirmText"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationConfirmText", EncodeHeaderValue(operationAttr.ConfirmText));
                }
                
                if (!string.IsNullOrEmpty(operationAttr.Icon) && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationIcon"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationIcon", EncodeHeaderValue(operationAttr.Icon));
                }
                
                if (operationAttr.IsBulkOperation && !context.HttpContext.Response.Headers.ContainsKey("X-Audit-IsBulkOperation"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-IsBulkOperation", "true");
                }
            }

            _logger.LogDebug("添加审计元数据到响应头: 操作={Operation}, 类型={Type}, 控制器={Controller}, 方法={Action}", 
                operationName, operationType, controllerDisplayName, methodDisplayName);
        }
        catch (Exception ex)
        {
            // 添加审计元数据失败不应影响主流程
            _logger.LogWarning(ex, "添加审计元数据到响应头失败");
        }
    }
}

