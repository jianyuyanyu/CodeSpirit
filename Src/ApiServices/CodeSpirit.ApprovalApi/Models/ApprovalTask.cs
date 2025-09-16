using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 审批任务
/// </summary>
public class ApprovalTask : AuditableEntityBase<long>, IMultiTenant
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
    /// 工作流节点ID
    /// </summary>
    [Required]
    public long WorkflowNodeId { get; set; }
    
    /// <summary>
    /// 审批人ID
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("审批人ID")]
    public string ApproverId { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批人姓名
    /// </summary>
    [StringLength(100)]
    [DisplayName("审批人姓名")]
    public string ApproverName { get; set; } = string.Empty;
    
    /// <summary>
    /// 任务状态
    /// </summary>
    [Required]
    [DisplayName("任务状态")]
    public ApprovalTaskStatus Status { get; set; } = ApprovalTaskStatus.Pending;
    
    /// <summary>
    /// 审批结果
    /// </summary>
    [DisplayName("审批结果")]
    public ApprovalResult? Result { get; set; }
    
    /// <summary>
    /// 审批意见
    /// </summary>
    [StringLength(1000)]
    [DisplayName("审批意见")]
    public string Comment { get; set; } = string.Empty;
    
    /// <summary>
    /// 分配时间
    /// </summary>
    [Required]
    [DisplayName("分配时间")]
    public DateTime AssignedTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 处理时间
    /// </summary>
    [DisplayName("处理时间")]
    public DateTime? ProcessedTime { get; set; }
    
    /// <summary>
    /// 是否为加签任务
    /// </summary>
    [DisplayName("是否为加签任务")]
    public bool IsAdditionalSign { get; set; } = false;
    
    /// <summary>
    /// 加签发起人ID
    /// </summary>
    [DisplayName("加签发起人ID")]
    public string? AdditionalSignInitiatorId { get; set; }
    
    /// <summary>
    /// 审批实例
    /// </summary>
    public virtual ApprovalInstance ApprovalInstance { get; set; } = null!;
}
