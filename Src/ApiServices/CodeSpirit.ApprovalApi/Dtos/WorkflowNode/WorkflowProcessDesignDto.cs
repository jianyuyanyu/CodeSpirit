namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流流程设计DTO
/// </summary>
public class WorkflowProcessDesignDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 流程配置（包含节点和连线信息的JSON）
    /// </summary>
    [Required]
    [DisplayName("流程配置")]
    public string ProcessConfig { get; set; } = string.Empty;

    /// <summary>
    /// 节点列表
    /// </summary>
    [Required]
    [DisplayName("节点列表")]
    public List<CreateWorkflowNodeDto> Nodes { get; set; } = new();
}
