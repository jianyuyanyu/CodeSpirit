using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 题目排序DTO（简化版本，仅用于排序）
/// </summary>
[DisplayName("题目排序")]
public class QuestionSortDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    [AmisFormField(Hidden = true)]
    public int Id { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    [AmisFormField(Hidden = true)]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool IsRequired { get; set; }
}
