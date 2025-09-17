namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 工作流定义查询DTO
/// </summary>
public class WorkflowDefinitionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 工作流名称（模糊查询）
    /// </summary>
    [DisplayName("工作流名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码（模糊查询）
    /// </summary>
    [DisplayName("工作流代码")]
    public string? Code { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    [DisplayName("版本")]
    public int? Version { get; set; }
}
