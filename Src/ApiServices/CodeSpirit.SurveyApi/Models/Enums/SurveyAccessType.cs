using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Models.Enums;

/// <summary>
/// 问卷访问类型枚举
/// </summary>
public enum SurveyAccessType
{
    /// <summary>
    /// 公开访问
    /// </summary>
    [Display(Name = "公开访问")]
    Public = 0,

    /// <summary>
    /// 需要登录
    /// </summary>
    [Display(Name = "需要登录")]
    Private = 1,

    /// <summary>
    /// 指定用户
    /// </summary>
    [Display(Name = "指定用户")]
    Restricted = 2
}
