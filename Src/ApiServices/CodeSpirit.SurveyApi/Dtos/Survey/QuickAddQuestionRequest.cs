using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 快速添加题目请求
/// </summary>
[DisplayName("快速添加题目")]
public class QuickAddQuestionRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 题目选项（用于单选、多选等题型）
    /// </summary>
    [DisplayName("题目选项")]
    public List<string>? Options { get; set; }
}
