using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Dtos.App;

/// <summary>
/// 提交问卷请求DTO
/// </summary>
[DisplayName("提交问卷")]
public class SubmitSurveyRequestDto
{
    /// <summary>
    /// 问卷ID（与AccessCode二选一）
    /// </summary>
    [DisplayName("问卷ID")]
    public int? SurveyId { get; set; }

    /// <summary>
    /// 公开访问码（与SurveyId二选一）
    /// </summary>
    [DisplayName("公开访问码")]
    [StringLength(16, MinimumLength = 4, ErrorMessage = "访问码长度必须在4-16个字符之间")]
    public string? AccessCode { get; set; }

    /// <summary>
    /// 会话ID（用于匿名用户标识）
    /// </summary>
    [DisplayName("会话ID")]
    [Required(ErrorMessage = "会话ID不能为空")]
    [StringLength(50, ErrorMessage = "会话ID长度不能超过50个字符")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 答题者ID（可为空，支持匿名用户）
    /// </summary>
    [DisplayName("答题者ID")]
    [StringLength(50, ErrorMessage = "答题者ID长度不能超过50个字符")]
    public string? RespondentId { get; set; }

    /// <summary>
    /// 回答列表
    /// </summary>
    [DisplayName("回答列表")]
    [Required(ErrorMessage = "回答列表不能为空")]
    public List<SubmitAnswerDto> Answers { get; set; } = new();

    /// <summary>
    /// 设备指纹
    /// </summary>
    [DisplayName("设备指纹")]
    [StringLength(100, ErrorMessage = "设备指纹长度不能超过100个字符")]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 元数据（JSON格式）
    /// </summary>
    [DisplayName("元数据")]
    [StringLength(2000, ErrorMessage = "元数据长度不能超过2000个字符")]
    public string? Metadata { get; set; }
}

/// <summary>
/// 提交答案DTO
/// </summary>
[DisplayName("提交答案")]
public class SubmitAnswerDto
{
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    [Required(ErrorMessage = "题目ID不能为空")]
    public int QuestionId { get; set; }

    /// <summary>
    /// 回答文本
    /// </summary>
    [DisplayName("回答文本")]
    [StringLength(4000, ErrorMessage = "回答文本长度不能超过4000个字符")]
    public string? AnswerText { get; set; }

    /// <summary>
    /// 回答值（用于选择题等，多个值用逗号分隔）
    /// </summary>
    [DisplayName("回答值")]
    [StringLength(2000, ErrorMessage = "回答值长度不能超过2000个字符")]
    public string? AnswerValue { get; set; }
}

/// <summary>
/// 提交问卷响应DTO
/// </summary>
[DisplayName("提交问卷响应")]
public class SubmitSurveyResponseDto
{
    /// <summary>
    /// 回答ID
    /// </summary>
    [DisplayName("回答ID")]
    public int ResponseId { get; set; }

    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [DisplayName("会话ID")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// 提交状态
    /// </summary>
    [DisplayName("提交状态")]
    public string Status { get; set; } = "已完成";
}
