using CodeSpirit.ApprovalApi.Models;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流节点预览DTO
/// </summary>
public class WorkflowNodePreviewDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    public WorkflowNodeType Type { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    public ApprovalMode ApprovalMode { get; set; }

    /// <summary>
    /// 节点配置
    /// </summary>
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批人列表
    /// </summary>
    public List<WorkflowNodeApproverPreviewDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件列表
    /// </summary>
    public List<WorkflowNodeConditionPreviewDto> Conditions { get; set; } = new();
}
