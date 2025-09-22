using CodeSpirit.ApprovalApi.Models;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流节点查询DTO
/// </summary>
public class WorkflowNodeQueryDto : QueryDtoBase
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [DisplayName("工作流定义ID")]
    public long? WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [StringLength(100)]
    [DisplayName("节点名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    [DisplayName("节点类型")]
    public WorkflowNodeType? NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode? ApprovalMode { get; set; }
}
