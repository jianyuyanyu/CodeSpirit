using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志摘要DTO
/// </summary>
public class AuditLogSummaryDto
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [DisplayName("日志ID")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [DisplayName("操作时间")]
    public DateTime OperationTime { get; set; }
    
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    public string OperationType { get; set; } = string.Empty;
    
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// HTTP状态码
    /// </summary>
    [DisplayName("HTTP状态码")]
    public int StatusCode { get; set; }
    
    /// <summary>
    /// 执行时长(毫秒)
    /// </summary>
    [DisplayName("执行时长(毫秒)")]
    public long ExecutionDuration { get; set; }
} 