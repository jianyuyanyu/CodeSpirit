using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services.Dtos;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 审计日志扩展方法
/// </summary>
public static class AuditLogExtensions
{
    /// <summary>
    /// 转换AuditLog到AuditLogDto
    /// </summary>
    /// <param name="auditLog">审计日志实体</param>
    /// <returns>审计日志DTO</returns>
    public static AuditLogDto ToDto(this AuditLog auditLog)
    {
        if (auditLog == null) return null;

        return new AuditLogDto
        {
            Id = auditLog.Id,
            TenantId = auditLog.TenantId ?? string.Empty,
            UserId = auditLog.UserId ?? string.Empty,
            UserName = auditLog.UserName ?? string.Empty,
            IpAddress = auditLog.IpAddress ?? string.Empty,
            OperationTime = auditLog.OperationTime,
            OperationName = auditLog.OperationName ?? string.Empty,
            OperationType = auditLog.OperationType ?? string.Empty,
            Description = auditLog.Description ?? string.Empty,
            RequestPath = auditLog.RequestPath ?? string.Empty,
            RequestMethod = auditLog.RequestMethod ?? string.Empty,
            ExecutionDuration = auditLog.ExecutionDuration,
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage ?? string.Empty,
            StatusCode = auditLog.StatusCode,
            
            // 新增字段映射
            UserAgent = auditLog.UserAgent ?? string.Empty,
            LocationInfo = auditLog.Location != null ? 
                $"{auditLog.Location.Country} {auditLog.Location.Region} {auditLog.Location.City}".Trim() : 
                string.Empty,
            RequestParams = auditLog.RequestParams ?? string.Empty,
            BeforeData = auditLog.BeforeData ?? string.Empty,
            AfterData = auditLog.AfterData ?? string.Empty,
            
            // 从 AdditionalData 中提取字段
            ApiController = auditLog.AdditionalData.TryGetValue("ApiController", out var controller) ? 
                controller?.ToString() ?? string.Empty : string.Empty,
            ApiAction = auditLog.AdditionalData.TryGetValue("ApiAction", out var action) ? 
                action?.ToString() ?? string.Empty : string.Empty,
            EntityName = auditLog.AdditionalData.TryGetValue("EntityName", out var entityName) ? 
                entityName?.ToString() ?? string.Empty : string.Empty,
            LogRequestParams = auditLog.AdditionalData.TryGetValue("LogRequestParams", out var logReqParams) ? 
                bool.TryParse(logReqParams?.ToString(), out var logReq) && logReq : false,
            LogResponseData = auditLog.AdditionalData.TryGetValue("LogResponseData", out var logRespData) ? 
                bool.TryParse(logRespData?.ToString(), out var logResp) && logResp : false,
            
            // 从 AttributeProperties 中提取字段
            OperationLabel = auditLog.AttributeProperties.TryGetValue("OperationLabel", out var opLabel) ? 
                opLabel ?? string.Empty : string.Empty,
            OperationActionType = auditLog.AttributeProperties.TryGetValue("OperationActionType", out var opActionType) ? 
                opActionType ?? string.Empty : string.Empty,
            OperationIcon = auditLog.AttributeProperties.TryGetValue("OperationIcon", out var opIcon) ? 
                opIcon ?? string.Empty : string.Empty,
            IsBulkOperation = auditLog.AttributeProperties.TryGetValue("IsBulkOperation", out var isBulk) ? 
                bool.TryParse(isBulk, out var bulk) && bulk : false,
            OperationConfirmText = auditLog.AttributeProperties.TryGetValue("OperationConfirmText", out var confirmText) ? 
                confirmText ?? string.Empty : string.Empty,
            OperationApi = auditLog.AttributeProperties.TryGetValue("OperationApi", out var opApi) ? 
                opApi ?? string.Empty : string.Empty
        };
    }

    /// <summary>
    /// 转换AuditLog集合到AuditLogDto集合
    /// </summary>
    /// <param name="auditLogs">审计日志实体集合</param>
    /// <returns>审计日志DTO集合</returns>
    public static IEnumerable<AuditLogDto> ToDto(this IEnumerable<AuditLog> auditLogs)
    {
        return auditLogs?.Select(x => x.ToDto()) ?? Enumerable.Empty<AuditLogDto>();
    }
} 