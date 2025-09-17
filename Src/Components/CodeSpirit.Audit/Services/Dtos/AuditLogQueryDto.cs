using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志查询参数
/// </summary>
public class AuditLogQueryDto : QueryDtoBase
{
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [StringLength(50, ErrorMessage = "租户ID长度不能超过50个字符")]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    [StringLength(50, ErrorMessage = "用户ID长度不能超过50个字符")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    [StringLength(100, ErrorMessage = "用户名长度不能超过100个字符")]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    [StringLength(45, ErrorMessage = "IP地址长度不能超过45个字符")]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
    
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [DisplayName("操作类型")]
    [StringLength(50, ErrorMessage = "操作类型长度不能超过50个字符")]
    public string OperationType { get; set; } = string.Empty;
    
    
    /// <summary>
    /// 是否成功
    /// </summary>
    [DisplayName("是否成功")]
    public bool? IsSuccess { get; set; }
} 