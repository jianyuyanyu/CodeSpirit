using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

/// <summary>
/// 工作流定义快速保存请求DTO
/// </summary>
public class WorkflowDefinitionQuickSaveRequestDto
{
    /// <summary>
    /// 修改的行数据
    /// </summary>
    public List<WorkflowDefinitionDto> Rows { get; set; } = new();

    /// <summary>
    /// 行差异数据
    /// </summary>
    public List<WorkflowDefinitionDiffDto> RowsDiff { get; set; } = new();

    /// <summary>
    /// ID字符串
    /// </summary>
    public string Ids { get; set; } = string.Empty;

    /// <summary>
    /// 未修改的项目
    /// </summary>
    public List<WorkflowDefinitionDto> UnModifiedItems { get; set; } = new();
}
