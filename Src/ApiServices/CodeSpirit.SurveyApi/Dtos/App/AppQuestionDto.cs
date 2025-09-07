using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.App;

/// <summary>
/// App端题目DTO
/// </summary>
[DisplayName("题目")]
public class AppQuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public int Id { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [DisplayName("题目描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool IsRequired { get; set; }

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    [DisplayName("验证规则")]
    public string? Validation { get; set; }

    /// <summary>
    /// 题目设置（JSON格式）
    /// </summary>
    [DisplayName("题目设置")]
    public string? Settings { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<AppQuestionOptionDto> Options { get; set; } = new();
}

/// <summary>
/// App端题目选项DTO
/// </summary>
[DisplayName("题目选项")]
public class AppQuestionOptionDto
{
    /// <summary>
    /// 选项ID
    /// </summary>
    [DisplayName("选项ID")]
    public int Id { get; set; }

    /// <summary>
    /// 选项文本
    /// </summary>
    [DisplayName("选项文本")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    [DisplayName("选项值")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否为其他选项
    /// </summary>
    [DisplayName("是否为其他选项")]
    public bool IsOther { get; set; }
}
