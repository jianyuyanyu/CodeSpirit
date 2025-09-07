using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.SurveyCategory;

/// <summary>
/// 问卷分类查询DTO
/// </summary>
public class SurveyCategoryQueryDto : QueryDtoBase
{
    /// <summary>
    /// 分类名称（模糊搜索）
    /// </summary>
    [DisplayName("分类名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("启用状态")]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/survey/SurveyCategories",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false,
        Clearable = true
    )]
    public int? ParentId { get; set; }

    /// <summary>
    /// 是否只查询顶级分类
    /// </summary>
    [DisplayName("只查询顶级分类")]
    public bool? OnlyTopLevel { get; set; }
}
