using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Settings.Enums;

/// <summary>
/// 题目唯一性校验模式枚举
/// </summary>
public enum QuestionUniquenessMode
{
    /// <summary>
    /// 不校验唯一性
    /// </summary>
    [Display(Name = "不校验")]
    None = 0,
    
    /// <summary>
    /// 全局唯一
    /// </summary>
    [Display(Name = "全局唯一")]
    Global = 1,
    
    /// <summary>
    /// 分类唯一
    /// </summary>
    [Display(Name = "分类唯一")]
    Category = 2
} 