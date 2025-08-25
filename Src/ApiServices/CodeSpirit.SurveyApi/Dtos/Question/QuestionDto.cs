using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 题目DTO
/// </summary>
[DisplayName("题目")]
public class QuestionDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public int Id { get; set; }

    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

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
    /// 验证规则
    /// </summary>
    [DisplayName("验证规则")]
    public string? Validation { get; set; }

    /// <summary>
    /// 题目设置
    /// </summary>
    [DisplayName("题目设置")]
    public string? Settings { get; set; }

    /// <summary>
    /// 是否由LLM生成
    /// </summary>
    [DisplayName("LLM生成")]
    public bool LLMGenerated { get; set; }

    /// <summary>
    /// 题目选项
    /// </summary>
    [DisplayName("题目选项")]
    public List<QuestionOptionDto> Options { get; set; } = new();
}
