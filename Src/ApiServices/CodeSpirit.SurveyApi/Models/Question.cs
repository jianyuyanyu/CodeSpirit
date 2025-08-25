using CodeSpirit.Shared.Entities;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 问卷题目实体
/// </summary>
public class Question : AuditableEntityBase<int>
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// 题目标题
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 题目描述
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 题目类型
    /// </summary>
    [Required]
    public QuestionType Type { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [Required]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    [StringLength(2000)]
    public string? Validation { get; set; }

    /// <summary>
    /// 题目设置（JSON格式）
    /// </summary>
    [StringLength(2000)]
    public string? Settings { get; set; }

    /// <summary>
    /// 是否由LLM生成
    /// </summary>
    public bool LLMGenerated { get; set; } = false;

    /// <summary>
    /// 关联的问卷
    /// </summary>
    public virtual Survey Survey { get; set; } = null!;

    /// <summary>
    /// 题目选项集合
    /// </summary>
    public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    /// <summary>
    /// 题目回答集合
    /// </summary>
    public virtual ICollection<ResponseAnswer> Answers { get; set; } = new List<ResponseAnswer>();
}
