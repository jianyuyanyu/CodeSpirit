using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services.Dtos;

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
            ServiceName = auditLog.ServiceName ?? string.Empty,
            ControllerName = auditLog.ControllerName ?? string.Empty,
            ActionName = auditLog.ActionName ?? string.Empty,
            OperationName = auditLog.OperationName ?? string.Empty,
            OperationType = auditLog.OperationType ?? string.Empty,
            Description = auditLog.Description ?? string.Empty,
            RequestPath = auditLog.RequestPath ?? string.Empty,
            RequestMethod = auditLog.RequestMethod ?? string.Empty,
            EntityName = auditLog.EntityName ?? string.Empty,
            EntityId = auditLog.EntityId ?? string.Empty,
            ExecutionDuration = auditLog.ExecutionDuration,
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage ?? string.Empty,
            StatusCode = auditLog.StatusCode
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