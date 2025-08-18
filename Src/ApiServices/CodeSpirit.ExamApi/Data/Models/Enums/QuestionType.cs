using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 题目类型
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
    /// 判断题
    /// </summary>
    [Display(Name = "判断题")]
    TrueFalse = 3,
    
    /// <summary>
    /// 填空题
    /// </summary>
    [Display(Name = "填空题")]
    FillBlank = 4,
    
    /// <summary>
    /// 简答题
    /// </summary>
    [Display(Name = "简答题")]
    ShortAnswer = 5,
    
    /// <summary>
    /// 问答题
    /// </summary>
    [Display(Name = "问答题")]
    Essay = 6
}
