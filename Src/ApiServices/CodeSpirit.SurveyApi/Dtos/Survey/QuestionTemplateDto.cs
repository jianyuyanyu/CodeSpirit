using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 题目模板DTO
/// </summary>
[DisplayName("题目模板")]
public class QuestionTemplateDto
{
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [DisplayName("模板名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    [DisplayName("图标")]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 默认选项（仅适用于选择题）
    /// </summary>
    [DisplayName("默认选项")]
    public string[]? DefaultOptions { get; set; }

    /// <summary>
    /// 默认设置（JSON格式）
    /// </summary>
    [DisplayName("默认设置")]
    public string? DefaultSettings { get; set; }
}
