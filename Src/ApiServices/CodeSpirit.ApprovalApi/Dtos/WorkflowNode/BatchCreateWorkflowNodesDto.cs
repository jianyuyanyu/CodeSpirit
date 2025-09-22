namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 批量创建工作流节点DTO
/// </summary>
public class BatchCreateWorkflowNodesDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 节点列表
    /// </summary>
    [Required]
    [DisplayName("节点列表")]
    public List<CreateWorkflowNodeDto> Nodes { get; set; } = new();
}
