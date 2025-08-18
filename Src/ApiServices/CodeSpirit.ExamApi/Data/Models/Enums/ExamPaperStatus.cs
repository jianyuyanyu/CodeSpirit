using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 试卷状态
/// </summary>
public enum ExamPaperStatus
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
    /// 已归档
    /// </summary>
    [Display(Name = "已归档")]
    Archived = 3
} 