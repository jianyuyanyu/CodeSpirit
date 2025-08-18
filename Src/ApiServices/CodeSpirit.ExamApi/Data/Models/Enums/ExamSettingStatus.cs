namespace CodeSpirit.ExamApi.Data.Models;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// 考试设置状态
/// </summary>
public enum ExamSettingStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    [Display(Name = "草稿")]
    Draft = 0,

    /// <summary>
    /// 已发布
    /// </summary>
    [Display(Name = "已发布")]
    Published = 1,

    /// <summary>
    /// 已结束
    /// </summary>
    [Display(Name = "已结束")]
    Ended = 2,

    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 3
} 