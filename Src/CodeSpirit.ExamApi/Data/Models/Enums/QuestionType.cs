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
    TrueFalse = 3
}
