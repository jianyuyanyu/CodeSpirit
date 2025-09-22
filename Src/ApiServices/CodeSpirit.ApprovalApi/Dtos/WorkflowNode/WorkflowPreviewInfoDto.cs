namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 工作流预览信息DTO
/// </summary>
public class WorkflowPreviewInfoDto
{
    /// <summary>
    /// 工作流ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工作流名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工作流描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 工作流配置
    /// </summary>
    public string Configuration { get; set; } = string.Empty;
}
