using Microsoft.AspNetCore.Mvc.Filters;
using System.ComponentModel;
using System.Reflection;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Models;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
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

            // 构建审计元数据对象
            var metadata = new AuditMetadata
            {
                OperationName = operationName,
                OperationType = operationType,
                Controller = controllerDisplayName,
                Action = methodDisplayName,
                Description = description
            };

            // 添加审计特性的额外字段
            if (auditAttr != null)
            {
                metadata.LogRequestParams = auditAttr.LogRequestParams;
                metadata.LogResponseData = auditAttr.LogResponseData;
                metadata.EntityName = auditAttr.EntityName;
                metadata.EntityIdParamName = auditAttr.EntityIdParamName;
            }

            // 添加操作特性信息
            if (operationAttr != null)
            {
                metadata.OperationLabel = operationAttr.Label;
                metadata.OperationActionType = operationAttr.ActionType;
                metadata.OperationApi = operationAttr.Api;
                metadata.OperationConfirmText = operationAttr.ConfirmText;
                metadata.OperationIcon = operationAttr.Icon;
                metadata.IsBulkOperation = operationAttr.IsBulkOperation;
            }

            // 序列化为JSON并Base64编码，添加到单个响应头
            try
            {
                var json = JsonConvert.SerializeObject(metadata);
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                
                // 只在响应头尚未设置时添加
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-Metadata"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-Metadata", base64);
                    _logger.LogDebug("添加审计元数据到响应头: 操作={Operation}, 类型={Type}, 控制器={Controller}, 方法={Action}", 
                        operationName, operationType, controllerDisplayName, methodDisplayName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "序列化审计元数据失败，回退到旧的多响应头方式");
                
                // 回退到旧方式（向后兼容）
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationName"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationName", EncodeHeaderValue(operationName));
                }
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Audit-OperationType"))
                {
                    context.HttpContext.Response.Headers.TryAdd("X-Audit-OperationType", EncodeHeaderValue(operationType));
                }
            }
        }
        catch (Exception ex)
        {
            // 添加审计元数据失败不应影响主流程
            _logger.LogWarning(ex, "添加审计元数据到响应头失败");
        }
    }
}

