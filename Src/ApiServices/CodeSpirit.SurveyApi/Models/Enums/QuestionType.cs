using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Models.Enums;

/// <summary>
/// 题目类型枚举
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 单选题
    /// </summary>
    [Display(Name = "单选题")]
    SingleChoice = 1,

    /// <summary>
    /// 多选题
    /// </summary>
    [Display(Name = "多选题")]
    MultipleChoice = 2,

    /// <summary>
    /// 填空题
    /// </summary>
    [Display(Name = "填空题")]
    Text = 3,

    /// <summary>
    /// 数字题
    /// </summary>
    [Display(Name = "数字题")]
    Number = 4,

    /// <summary>
    /// 评分题
    /// </summary>
    [Display(Name = "评分题")]
    Rating = 5,

    /// <summary>
    /// 日期题
    /// </summary>
    [Display(Name = "日期题")]
    Date = 6,

    /// <summary>
    /// 时间题
    /// </summary>
    [Display(Name = "时间题")]
    Time = 7,

    /// <summary>
    /// 日期时间题
    /// </summary>
    [Display(Name = "日期时间题")]
    DateTime = 8,

    /// <summary>
    /// 长文本题
    /// </summary>
    [Display(Name = "长文本题")]
    Textarea = 9,

    /// <summary>
    /// 矩阵题
    /// </summary>
    [Display(Name = "矩阵题")]
    Matrix = 10,

    /// <summary>
    /// 排序题
    /// </summary>
    [Display(Name = "排序题")]
    Ranking = 11
}
