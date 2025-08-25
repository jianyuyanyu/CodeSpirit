namespace CodeSpirit.SurveyApi.Dtos.Question;

/// <summary>
/// 题目选项DTO
/// </summary>
[DisplayName("题目选项")]
public class QuestionOptionDto
{
    /// <summary>
    /// 选项ID
    /// </summary>
    [DisplayName("选项ID")]
    public int Id { get; set; }

    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public int QuestionId { get; set; }

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

    /// <summary>
    /// 是否为"其他"选项
    /// </summary>
    [DisplayName("其他选项")]
    public bool IsOther { get; set; }
}
