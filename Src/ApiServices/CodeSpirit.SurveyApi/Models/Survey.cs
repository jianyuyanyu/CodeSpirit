using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 问卷实体
/// </summary>
public class Survey : AuditableEntityBase<int>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 问卷描述
    /// </summary>
    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// 问卷状态
    /// </summary>
    [Required]
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    /// <summary>
    /// 访问类型
    /// </summary>
    [Required]
    public SurveyAccessType AccessType { get; set; } = SurveyAccessType.Public;

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 问卷设置（JSON格式）
    /// </summary>
    [StringLength(4000)]
    public string? Settings { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    public bool IsTemplate { get; set; } = false;

    /// <summary>
    /// LLM生成提示词
    /// </summary>
    [StringLength(4000)]
    public string? LLMPrompt { get; set; }

    /// <summary>
    /// LLM原始输出内容
    /// </summary>
    [StringLength(8000)]
    public string? LLMRawOutput { get; set; }

    /// <summary>
    /// 是否已预览
    /// </summary>
    public bool IsPreviewChecked { get; set; } = false;

    /// <summary>
    /// 问卷分类ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// 问卷分类
    /// </summary>
    public virtual SurveyCategory? Category { get; set; }

    /// <summary>
    /// 问卷题目集合
    /// </summary>
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    /// <summary>
    /// 问卷回答集合
    /// </summary>
    public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();

    /// <summary>
    /// 问卷草稿集合
    /// </summary>
    public virtual ICollection<SurveyDraft> Drafts { get; set; } = new List<SurveyDraft>();
}
