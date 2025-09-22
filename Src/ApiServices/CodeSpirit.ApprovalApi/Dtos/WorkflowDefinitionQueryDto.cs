using System.ComponentModel;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 工作流定义查询DTO
/// </summary>
[DisplayName("工作流定义查询")]
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

    /// <summary>
    /// 流程分类ID
    /// </summary>
    [DisplayName("流程分类")]
    [PageAside]
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/approval/WorkflowCategories/tree",
        Multiple = false,
        JoinValues = true,
        ExtractValue = false,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Required = false,
        ShowIcon = true,
        Clearable = true,
        SubmitOnChange = true,
        HeightAuto = true,
        SelectFirst = false
    )]
    public int? CategoryId { get; set; }
}
