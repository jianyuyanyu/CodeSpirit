using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 工作流节点审批人
/// </summary>
public class WorkflowNodeApprover : EntityBase<long>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流节点ID
    /// </summary>
    [Required]
    public long WorkflowNodeId { get; set; }
    
    /// <summary>
    /// 审批人类型
    /// </summary>
    [Required]
    [DisplayName("审批人类型")]
    public ApproverType ApproverType { get; set; }
    
    /// <summary>
    /// 审批人值（用户ID、角色ID、部门ID等）
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("审批人值")]
    public string ApproverValue { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批人名称
    /// </summary>
    [StringLength(100)]
    [DisplayName("审批人名称")]
    public string ApproverName { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流节点
    /// </summary>
    public virtual WorkflowNode WorkflowNode { get; set; } = null!;
}
