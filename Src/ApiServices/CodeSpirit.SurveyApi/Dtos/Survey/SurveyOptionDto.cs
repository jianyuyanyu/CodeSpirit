using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷选项DTO（用于下拉选择）
/// </summary>
[DisplayName("问卷选项")]
public class SurveyOptionDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int Id { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷状态
    /// </summary>
    [DisplayName("问卷状态")]
    public string Status { get; set; } = string.Empty;
}
