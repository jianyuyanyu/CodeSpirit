namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流节点条件DTO
/// </summary>
public class WorkflowNodeConditionDto
{
    /// <summary>
    /// 条件ID
    /// </summary>
    [DisplayName("条件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 条件表达式
    /// </summary>
    [DisplayName("条件表达式")]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 下一个节点名称
    /// </summary>
    [DisplayName("下一个节点名称")]
    public string NextNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 条件描述
    /// </summary>
    [DisplayName("条件描述")]
    public string Description { get; set; } = string.Empty;
}
