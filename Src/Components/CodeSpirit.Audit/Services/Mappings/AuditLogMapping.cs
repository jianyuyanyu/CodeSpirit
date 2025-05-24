using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services.Mappings;

/// <summary>
/// 审计日志映射类
/// </summary>
public static class AuditLogMapping
{
    /// <summary>
    /// 将AuditLogQueryDto转换为搜索参数
    /// </summary>
    /// <param name="queryDto">查询DTO</param>
    /// <returns>搜索参数</returns>
    public static Dictionary<string, object> ToSearchParameters(this AuditLogQueryDto queryDto)
    {
        var parameters = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(queryDto.UserId))
            parameters["UserId"] = queryDto.UserId;
            
        if (!string.IsNullOrEmpty(queryDto.UserName))
            parameters["UserName"] = queryDto.UserName;
            
        if (!string.IsNullOrEmpty(queryDto.IpAddress))
            parameters["IpAddress"] = queryDto.IpAddress;
            
        if (queryDto.StartTime.HasValue)
            parameters["StartTime"] = queryDto.StartTime.Value;
            
        if (queryDto.EndTime.HasValue)
            parameters["EndTime"] = queryDto.EndTime.Value;
            
        if (!string.IsNullOrEmpty(queryDto.ServiceName))
            parameters["ServiceName"] = queryDto.ServiceName;
            
        if (!string.IsNullOrEmpty(queryDto.ControllerName))
            parameters["ControllerName"] = queryDto.ControllerName;
            
        if (!string.IsNullOrEmpty(queryDto.ActionName))
            parameters["ActionName"] = queryDto.ActionName;
            
        if (!string.IsNullOrEmpty(queryDto.OperationType))
            parameters["OperationType"] = queryDto.OperationType;
            
        if (!string.IsNullOrEmpty(queryDto.EntityName))
            parameters["EntityName"] = queryDto.EntityName;
            
        if (!string.IsNullOrEmpty(queryDto.EntityId))
            parameters["EntityId"] = queryDto.EntityId;
            
        if (queryDto.IsSuccess.HasValue)
            parameters["IsSuccess"] = queryDto.IsSuccess.Value;
            
        if (!string.IsNullOrEmpty(queryDto.Keywords))
            parameters["Keyword"] = queryDto.Keywords;
            
        parameters["PageIndex"] = queryDto.PageIndex;
        parameters["PageSize"] = queryDto.PageSize;
        parameters["SortField"] = queryDto.SortField;
        parameters["SortDirection"] = queryDto.SortDirection;
        
        return parameters;
    }
    
    /// <summary>
    /// 创建审计日志摘要DTO
    /// </summary>
    /// <param name="auditLog">审计日志模型</param>
    /// <returns>审计日志摘要DTO</returns>
    public static AuditLogSummaryDto ToSummaryDto(this AuditLog auditLog)
    {
        return new AuditLogSummaryDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            IpAddress = auditLog.IpAddress,
            OperationTime = auditLog.OperationTime,
            ServiceName = auditLog.ServiceName,
            ControllerName = auditLog.ControllerName,
            ActionName = auditLog.ActionName,
            OperationType = auditLog.OperationType,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            IsSuccess = auditLog.IsSuccess,
            StatusCode = auditLog.StatusCode,
            ExecutionDuration = auditLog.ExecutionDuration
        };
    }
    
    /// <summary>
    /// 创建审计日志详情DTO
    /// </summary>
    /// <param name="auditLog">审计日志模型</param>
    /// <returns>审计日志详情DTO</returns>
    public static AuditLogDetailDto ToDetailDto(this AuditLog auditLog)
    {
        return new AuditLogDetailDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            IpAddress = auditLog.IpAddress,
            Location = auditLog.Location,
            UserAgent = auditLog.UserAgent,
            OperationTime = auditLog.OperationTime,
            ServiceName = auditLog.ServiceName,
            ControllerName = auditLog.ControllerName,
            ActionName = auditLog.ActionName,
            OperationName = auditLog.OperationName,
            OperationType = auditLog.OperationType,
            Description = auditLog.Description,
            RequestPath = auditLog.RequestPath,
            RequestMethod = auditLog.RequestMethod,
            RequestParams = auditLog.RequestParams,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            BeforeData = auditLog.BeforeData,
            AfterData = auditLog.AfterData,
            ExecutionDuration = auditLog.ExecutionDuration,
            IsSuccess = auditLog.IsSuccess,
            ErrorMessage = auditLog.ErrorMessage,
            StatusCode = auditLog.StatusCode,
            AttributeProperties = auditLog.AttributeProperties,
            AdditionalData = auditLog.AdditionalData
        };
    }
} 