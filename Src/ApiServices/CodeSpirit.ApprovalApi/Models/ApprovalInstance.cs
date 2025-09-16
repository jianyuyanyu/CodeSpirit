using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 审批实例
/// </summary>
public class ApprovalInstance : AuditableEntityBase<long>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    public long WorkflowDefinitionId { get; set; }
    
    /// <summary>
    /// 审批标题
    /// </summary>
    [Required]
    [StringLength(200)]
    [DisplayName("审批标题")]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体类型（微服务名称.实体类型，如：ExamApi.ExamSession）
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("业务实体类型")]
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// 业务实体ID
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("业务实体ID")]
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 申请人ID
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("申请人ID")]
    public string ApplicantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 申请人姓名
    /// </summary>
    [StringLength(100)]
    [DisplayName("申请人姓名")]
    public string ApplicantName { get; set; } = string.Empty;
    
    /// <summary>
    /// 当前状态
    /// </summary>
    [Required]
    [DisplayName("当前状态")]
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// 当前节点ID
    /// </summary>
    [DisplayName("当前节点ID")]
    public long? CurrentNodeId { get; set; }
    
    /// <summary>
    /// 申请时间
    /// </summary>
    [Required]
    [DisplayName("申请时间")]
    public DateTime ApplyTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 完成时间
    /// </summary>
    [DisplayName("完成时间")]
    public DateTime? CompletedTime { get; set; }
    
    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    [DisplayName("业务数据")]
    public string BusinessData { get; set; } = string.Empty;
    
    /// <summary>
    /// 风险评估结果（JSON格式）
    /// </summary>
    [DisplayName("风险评估结果")]
    public string? RiskAssessmentResult { get; set; }
    
    /// <summary>
    /// 智能审批建议（JSON格式）
    /// </summary>
    [DisplayName("智能审批建议")]
    public string? IntelligentSuggestion { get; set; }
    
    /// <summary>
    /// 审批任务集合
    /// </summary>
    public virtual ICollection<ApprovalTask> Tasks { get; set; } = new List<ApprovalTask>();
    
    /// <summary>
    /// 工作流定义
    /// </summary>
    public virtual WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}
