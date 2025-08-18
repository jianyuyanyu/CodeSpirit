using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 练习模式
/// </summary>
public enum PracticeMode
{
    /// <summary>
    /// 顺序练习
    /// </summary>
    [Display(Name = "顺序练习")]
    Sequential = 1,
    
    /// <summary>
    /// 随机练习
    /// </summary>
    [Display(Name = "随机练习")]
    Random = 2,
    
    /// <summary>
    /// 模拟考试
    /// </summary>
    [Display(Name = "模拟考试")]
    MockExam = 3,
    
    /// <summary>
    /// 错题练习
    /// </summary>
    [Display(Name = "错题练习")]
    WrongQuestions = 4,
    
    /// <summary>
    /// 自由练习
    /// </summary>
    [Display(Name = "自由练习")]
    Free = 5
} 