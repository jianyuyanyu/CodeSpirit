using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.SurveyCategory;

/// <summary>
/// 问卷分类DTO
/// </summary>
public class SurveyCategoryDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [DisplayName("分类ID")]
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [DisplayName("分类名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    [DisplayName("分类描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 分类颜色
    /// </summary>
    [DisplayName("分类颜色")]
    public string? Color { get; set; }

    /// <summary>
    /// 分类图标
    /// </summary>
    [DisplayName("分类图标")]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("启用状态")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类ID")]
    public int? ParentId { get; set; }

    /// <summary>
    /// 父级分类名称
    /// </summary>
    [DisplayName("父级分类")]
    public string? ParentName { get; set; }

    /// <summary>
    /// 问卷数量
    /// </summary>
    [DisplayName("问卷数量")]
    public int SurveyCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [DisplayName("创建人")]
    public string? CreatedBy { get; set; }
}
