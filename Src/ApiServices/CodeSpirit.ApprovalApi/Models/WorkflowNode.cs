using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 工作流节点
/// </summary>
public class WorkflowNode : EntityBase<long>, IMultiTenant
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
    /// 节点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    [DisplayName("节点类型")]
    public WorkflowNodeType NodeType { get; set; }
    
    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Sequential;
    
    /// <summary>
    /// 节点配置（JSON格式）
    /// </summary>
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批人配置
    /// </summary>
    public virtual ICollection<WorkflowNodeApprover> Approvers { get; set; } = new List<WorkflowNodeApprover>();
    
    /// <summary>
    /// 条件配置
    /// </summary>
    public virtual ICollection<WorkflowNodeCondition> Conditions { get; set; } = new List<WorkflowNodeCondition>();
    
    /// <summary>
    /// 工作流定义
    /// </summary>
    public virtual WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}
