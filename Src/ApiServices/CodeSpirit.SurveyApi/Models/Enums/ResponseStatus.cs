using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Models.Enums;

/// <summary>
/// 回答状态枚举
/// </summary>
public enum ResponseStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    [Display(Name = "进行中")]
    InProgress = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    [Display(Name = "已完成")]
    Completed = 1,

    /// <summary>
    /// 已放弃
    /// </summary>
    [Display(Name = "已放弃")]
    Abandoned = 2
}
