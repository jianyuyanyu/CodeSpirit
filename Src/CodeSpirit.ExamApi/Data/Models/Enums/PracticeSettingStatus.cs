using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 练习设置状态
/// </summary>
public enum PracticeSettingStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    [Display(Name = "草稿")]
    Draft = 1,
    
    /// <summary>
    /// 已发布
    /// </summary>
    [Display(Name = "已发布")]
    Published = 2,
    
    /// <summary>
    /// 已禁用
    /// </summary>
    [Display(Name = "已禁用")]
    Disabled = 3
} 