using CodeSpirit.ApprovalApi.Models;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流节点审批人预览DTO
/// </summary>
public class WorkflowNodeApproverPreviewDto
{
    /// <summary>
    /// 审批人类型
    /// </summary>
    public ApproverType Type { get; set; }

    /// <summary>
    /// 审批人值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 审批人名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
