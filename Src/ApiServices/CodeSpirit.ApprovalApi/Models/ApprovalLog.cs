using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 审批日志
/// </summary>
public class ApprovalLog : EntityBase<string>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批实例ID
    /// </summary>
    [Required]
    public long ApprovalInstanceId { get; set; }
    
    /// <summary>
    /// 任务ID（可选）
    /// </summary>
    public long? TaskId { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    [DisplayName("操作类型")]
    public ApprovalLogType LogType { get; set; }
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("操作人ID")]
    public string OperatorId { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作人姓名
    /// </summary>
    [StringLength(100)]
    [DisplayName("操作人姓名")]
    public string OperatorName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [Required]
    [DisplayName("操作时间")]
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 操作结果
    /// </summary>
    [DisplayName("操作结果")]
    public ApprovalResult? Result { get; set; }
    
    /// <summary>
    /// 操作内容/意见
    /// </summary>
    [StringLength(1000)]
    [DisplayName("操作内容")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [DisplayName("扩展数据")]
    public string ExtensionData { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    [DisplayName("IP地址")]
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    [DisplayName("用户代理")]
    public string UserAgent { get; set; } = string.Empty;
}
