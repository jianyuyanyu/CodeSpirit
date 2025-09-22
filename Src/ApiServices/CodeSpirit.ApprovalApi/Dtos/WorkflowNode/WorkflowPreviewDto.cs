namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流预览数据DTO
/// </summary>
public class WorkflowPreviewDto
{
    /// <summary>
    /// 工作流信息
    /// </summary>
    public WorkflowPreviewInfoDto Workflow { get; set; } = new();

    /// <summary>
    /// 节点列表
    /// </summary>
    public List<WorkflowNodePreviewDto> Nodes { get; set; } = new();
}
