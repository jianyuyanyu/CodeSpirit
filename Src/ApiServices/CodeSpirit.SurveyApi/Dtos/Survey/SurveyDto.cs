using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using CodeSpirit.SurveyApi.Models.Enums;
using System.ComponentModel;

namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷DTO
/// </summary>
[DisplayName("问卷")]
public class SurveyDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [AmisColumn(Hidden = true)]
    public string TenantId { get; set; } = string.Empty;

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
    /// 问卷状态
    /// </summary>
    [DisplayName("问卷状态")]
    public SurveyStatus Status { get; set; }

    /// <summary>
    /// 访问类型
    /// </summary>
    [DisplayName("访问类型")]
    public SurveyAccessType AccessType { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [DisplayName("发布时间")]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否为模板
    /// </summary>
    [DisplayName("是否为模板")]
    public bool IsTemplate { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [DisplayName("创建者")]
    public long CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; }

    /// <summary>
    /// 回答数量
    /// </summary>
    [DisplayName("回答数量")]
    public int ResponseCount { get; set; }

    /// <summary>
    /// 是否已预览
    /// </summary>
    [DisplayName("是否已预览")]
    public bool IsPreviewChecked { get; set; }

    /// <summary>
    /// 问卷分类ID
    /// </summary>
    [DisplayName("分类ID")]
    [AmisColumn(Hidden = true)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// 问卷分类名称
    /// </summary>
    [DisplayName("问卷分类")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// 公开访问码
    /// </summary>
    [DisplayName("访问码")]
    public string? AccessCode { get; set; }

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    [DisplayName("预计用时")]
    public int? EstimatedMinutes { get; set; }
}
