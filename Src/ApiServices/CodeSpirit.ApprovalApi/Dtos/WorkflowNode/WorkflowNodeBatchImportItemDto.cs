namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流节点批量导入项DTO
/// </summary>
public class WorkflowNodeBatchImportItemDto
{
    /// <summary>
    /// 节点名称
    /// </summary>
    [Required]
    [JsonProperty("节点名称")]
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    [JsonProperty("节点类型")]
    [DisplayName("节点类型")]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// 审批模式
    /// </summary>
    [JsonProperty("审批模式")]
    [DisplayName("审批模式")]
    public string ApprovalMode { get; set; } = string.Empty;

    /// <summary>
    /// 节点配置
    /// </summary>
    [JsonProperty("节点配置")]
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = "{}";
}
