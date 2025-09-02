using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 生成的问卷DTO
/// </summary>
[DisplayName("生成的问卷")]
public class GeneratedSurveyDto
{
    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [DisplayName("问卷描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 生成的题目列表
    /// </summary>
    [DisplayName("生成的题目列表")]
    public List<GeneratedQuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// 使用的提示词
    /// </summary>
    [DisplayName("使用的提示词")]
    public string? UsedPrompt { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    [DisplayName("生成时间")]
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 生成质量评分（1-10）
    /// </summary>
    [DisplayName("生成质量评分")]
    public int QualityScore { get; set; }

    /// <summary>
    /// 保存后的问卷ID
    /// </summary>
    [DisplayName("保存后的问卷ID")]
    public int? SavedSurveyId { get; set; }
}

/// <summary>
/// 生成的题目DTO
/// </summary>
[DisplayName("生成的题目")]
public class GeneratedQuestionDto
{
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
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 是否必填
    /// </summary>
    [DisplayName("是否必填")]
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<GeneratedQuestionOptionDto> Options { get; set; } = new();


    public bool LLMGenerated { get; set; }
}

/// <summary>
/// 生成的题目选项DTO
/// </summary>
[DisplayName("生成的题目选项")]
public class GeneratedQuestionOptionDto
{
    /// <summary>
    /// 选项文本
    /// </summary>
    [DisplayName("选项文本")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    [DisplayName("选项值")]
    public string? Value { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序索引")]
    public int OrderIndex { get; set; }
}
