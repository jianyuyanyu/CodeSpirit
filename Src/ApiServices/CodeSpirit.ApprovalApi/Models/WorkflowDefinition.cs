using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;

namespace CodeSpirit.ApprovalApi.Models;

/// <summary>
/// 工作流定义
/// </summary>
public class WorkflowDefinition : AuditableEntityBase<long>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("工作流名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流代码（唯一标识）
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("工作流代码")]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流描述
    /// </summary>
    [StringLength(500)]
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 工作流版本
    /// </summary>
    [Required]
    [DisplayName("版本")]
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 工作流配置（JSON格式）
    /// </summary>
    [Required]
    [DisplayName("工作流配置")]
    public string Configuration { get; set; } = string.Empty;
    
    /// <summary>
    /// 审批表单Schema（符合AMIS要求的JSON结构）
    /// </summary>
    [DisplayName("审批表单Schema")]
    [StringLength(int.MaxValue)]
    public string? FormSchema { get; set; }
    
    /// <summary>
    /// 流程分类ID
    /// </summary>
    [DisplayName("流程分类")]
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// 流程分类
    /// </summary>
    public virtual WorkflowCategory? Category { get; set; }
    
    /// <summary>
    /// 工作流节点集合
    /// </summary>
    public virtual ICollection<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
}
