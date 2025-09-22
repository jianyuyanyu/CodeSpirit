namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 前端条件DTO
/// </summary>
public class FrontendConditionDto
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 下一节点名称
    /// </summary>
    public string NextNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 条件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}