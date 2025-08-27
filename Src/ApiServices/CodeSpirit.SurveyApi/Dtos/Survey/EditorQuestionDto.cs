using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 编辑器题目DTO
/// </summary>
[DisplayName("编辑器题目")]
public class EditorQuestionDto
{
    /// <summary>
    /// 题目ID（0表示新建）
    /// </summary>
    [DisplayName("题目ID")]
    public int Id { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("题目标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
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
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    [StringLength(2000)]
    [DisplayName("验证规则")]
    public string? Validation { get; set; }

    /// <summary>
    /// 题目设置（JSON格式）
    /// </summary>
    [StringLength(2000)]
    [DisplayName("题目设置")]
    public string? Settings { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<EditorQuestionOptionDto>? Options { get; set; }
}

/// <summary>
/// 编辑器题目选项DTO
/// </summary>
[DisplayName("编辑器题目选项")]
public class EditorQuestionOptionDto
{
    /// <summary>
    /// 选项ID（0表示新建）
    /// </summary>
    [DisplayName("选项ID")]
    public int Id { get; set; }

    /// <summary>
    /// 选项文本
    /// </summary>
    [Required]
    [StringLength(500)]
    [DisplayName("选项文本")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    [StringLength(200)]
    [DisplayName("选项值")]
    public string? Value { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否为其他选项
    /// </summary>
    [DisplayName("其他选项")]
    public bool IsOther { get; set; } = false;
}
