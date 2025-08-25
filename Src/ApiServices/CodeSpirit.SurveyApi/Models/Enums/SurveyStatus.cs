namespace CodeSpirit.SurveyApi.Models.Enums;

/// <summary>
/// 问卷状态枚举
/// </summary>
public enum SurveyStatus
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
    /// 已关闭
    /// </summary>
    [Display(Name = "已关闭")]
    Closed = 2,

    /// <summary>
    /// 已归档
    /// </summary>
    [Display(Name = "已归档")]
    Archived = 3
}
