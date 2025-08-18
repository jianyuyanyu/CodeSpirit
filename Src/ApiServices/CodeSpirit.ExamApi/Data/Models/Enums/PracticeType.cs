using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 练习类型
/// </summary>
public enum PracticeType
{
    /// <summary>
    /// 普通练习
    /// </summary>
    [Display(Name = "普通练习")]
    Normal = 1,
    
    /// <summary>
    /// 模拟考试
    /// </summary>
    [Display(Name = "模拟考试")]
    MockExam = 2,
    
    /// <summary>
    /// 错题练习
    /// </summary>
    [Display(Name = "错题练习")]
    WrongQuestions = 3,
    
    /// <summary>
    /// 自由练习
    /// </summary>
    [Display(Name = "自由练习")]
    Free = 4,
    
    /// <summary>
    /// 定向练习
    /// </summary>
    [Display(Name = "定向练习")]
    Directed = 5
}
