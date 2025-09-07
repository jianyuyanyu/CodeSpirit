using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.Response;

/// <summary>
/// 问卷回答查询DTO
/// </summary>
[DisplayName("问卷回答查询")]
public class ResponseQueryDto : QueryDtoBase
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int? SurveyId { get; set; }

    /// <summary>
    /// 问卷标题（模糊查询）
    /// </summary>
    [DisplayName("问卷标题")]
    public string? SurveyTitle { get; set; }

    /// <summary>
    /// 答题者ID
    /// </summary>
    [DisplayName("答题者ID")]
    public string? RespondentId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [DisplayName("会话ID")]
    public string? SessionId { get; set; }

    /// <summary>
    /// 回答状态
    /// </summary>
    [DisplayName("状态")]
    public ResponseStatus? Status { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 开始时间范围 - 开始
    /// </summary>
    [DisplayName("开始时间（从）")]
    public DateTime? StartedAtFrom { get; set; }

    /// <summary>
    /// 开始时间范围 - 结束
    /// </summary>
    [DisplayName("开始时间（到）")]
    public DateTime? StartedAtTo { get; set; }

    /// <summary>
    /// 完成时间范围 - 开始
    /// </summary>
    [DisplayName("完成时间（从）")]
    public DateTime? CompletedAtFrom { get; set; }

    /// <summary>
    /// 完成时间范围 - 结束
    /// </summary>
    [DisplayName("完成时间（到）")]
    public DateTime? CompletedAtTo { get; set; }

    /// <summary>
    /// 是否包含答案详情
    /// </summary>
    [DisplayName("包含答案详情")]
    public bool IncludeAnswers { get; set; } = false;
}
