using CodeSpirit.Shared.Entities;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 题目选项实体
/// </summary>
public class QuestionOption : EntityBase<int>
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required]
    public int QuestionId { get; set; }

    /// <summary>
    /// 选项文本
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 选项值
    /// </summary>
    [StringLength(200)]
    public string? Value { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [Required]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否为"其他"选项
    /// </summary>
    public bool IsOther { get; set; } = false;

    /// <summary>
    /// 关联的题目
    /// </summary>
    public virtual Question Question { get; set; } = null!;
}
