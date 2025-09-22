namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 创建工作流节点条件DTO
/// </summary>
public class CreateWorkflowNodeConditionDto
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "条件表达式长度不能超过500个字符")]
    [DisplayName("条件表达式")]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 下一个节点名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "下一个节点名称长度不能超过100个字符")]
    [DisplayName("下一个节点名称")]
    public string NextNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 条件描述
    /// </summary>
    [StringLength(200, ErrorMessage = "条件描述长度不能超过200个字符")]
    [DisplayName("条件描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 排序
    /// </summary>
    [DisplayName("排序")]
    public int Order { get; set; } = 0;
}
