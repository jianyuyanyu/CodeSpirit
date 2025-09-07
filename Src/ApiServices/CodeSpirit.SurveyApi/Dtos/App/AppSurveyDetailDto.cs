using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.App;

/// <summary>
/// App端问卷详情DTO
/// </summary>
[DisplayName("问卷详情")]
public class AppSurveyDetailDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int Id { get; set; }

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
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; }

    /// <summary>
    /// 预计完成时间（分钟）
    /// </summary>
    [DisplayName("预计完成时间")]
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// 公开访问码
    /// </summary>
    [DisplayName("访问码")]
    public string PublicAccessCode { get; set; } = string.Empty;

    /// <summary>
    /// 问卷分类名称
    /// </summary>
    [DisplayName("问卷分类")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// 问卷题目列表
    /// </summary>
    [DisplayName("问卷题目")]
    public List<AppQuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// 是否已过期
    /// </summary>
    [DisplayName("是否已过期")]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// 是否可以参与
    /// </summary>
    [DisplayName("是否可以参与")]
    public bool CanParticipate => Status == SurveyStatus.Published && !IsExpired;
}
