namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 前端预览数据DTO
/// </summary>
public class FrontendPreviewDataDto
{
    /// <summary>
    /// 工作流信息
    /// </summary>
    public WorkflowPreviewInfoDto Workflow { get; set; } = new();

    /// <summary>
    /// 节点列表
    /// </summary>
    public List<FrontendNodeDto> Nodes { get; set; } = new();
}
