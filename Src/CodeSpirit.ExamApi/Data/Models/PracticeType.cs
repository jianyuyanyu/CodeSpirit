using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 练习类型
/// </summary>
public enum PracticeType
{
    /// <summary>
    /// 自由练习
    /// </summary>
    [Display(Name = "自由练习")]
    FreePractice = 1,
    
    /// <summary>
    /// 模拟考试
    /// </summary>
    [Display(Name = "模拟考试")]
    MockExam = 2
}
