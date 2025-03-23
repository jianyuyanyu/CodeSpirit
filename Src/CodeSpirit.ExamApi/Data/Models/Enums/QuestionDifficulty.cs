using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 题目难度
/// </summary>
public enum QuestionDifficulty
{
    /// <summary>
    /// 简单
    /// </summary>
    [Display(Name = "简单")]
    Easy = 1,
    
    /// <summary>
    /// 中等
    /// </summary>
    [Display(Name = "中等")]
    Medium = 2,
    
    /// <summary>
    /// 困难
    /// </summary>
    [Display(Name = "困难")]
    Hard = 3
}
