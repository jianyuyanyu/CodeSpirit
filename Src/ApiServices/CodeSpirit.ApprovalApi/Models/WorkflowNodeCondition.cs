using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 工作流节点条件
/// </summary>
public class WorkflowNodeCondition : EntityBase<long>, IMultiTenant
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
    /// 条件表达式
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("条件表达式")]
    public string Expression { get; set; } = string.Empty;
    
    /// <summary>
    /// 下一个节点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("下一个节点名称")]
    public string NextNodeName { get; set; } = string.Empty;
    
    /// <summary>
    /// 条件描述
    /// </summary>
    [StringLength(200)]
    [DisplayName("条件描述")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 排序
    /// </summary>
    [DisplayName("排序")]
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// 工作流节点
    /// </summary>
    public virtual WorkflowNode WorkflowNode { get; set; } = null!;
}
