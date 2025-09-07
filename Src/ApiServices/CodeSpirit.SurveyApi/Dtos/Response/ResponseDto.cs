using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Dtos.Response;

/// <summary>
/// 问卷回答DTO
/// </summary>
[DisplayName("问卷回答")]
public class ResponseDto
{
    /// <summary>
    /// 回答ID
    /// </summary>
    [DisplayName("回答ID")]
    public int Id { get; set; }

    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    [AggregateField(dataSource: "/api/surveys/{value}.title")]
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
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 开始答题时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 完成答题时间
    /// </summary>
    [DisplayName("完成时间")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 回答状态
    /// </summary>
    [DisplayName("状态")]
    public ResponseStatus Status { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [DisplayName("用户代理")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 设备指纹
    /// </summary>
    [DisplayName("设备指纹")]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 答题用时（分钟）
    /// </summary>
    [DisplayName("答题用时")]
    public int? DurationMinutes { get; set; }

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
    /// 回答详情列表
    /// </summary>
    [DisplayName("回答详情")]
    public List<ResponseAnswerDto>? Answers { get; set; }
}
