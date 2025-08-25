using CodeSpirit.Shared.Entities;
using CodeSpirit.SurveyApi.Models.Enums;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 问卷回答实体
/// </summary>
public class SurveyResponse : AuditableEntityBase<int>
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// 答题者ID（可为空，支持匿名用户）
    /// </summary>
    [StringLength(50)]
    public string? RespondentId { get; set; }

    /// <summary>
    /// 会话ID（用于匿名用户标识）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 开始答题时间
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 完成答题时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 回答状态
    /// </summary>
    [Required]
    public ResponseStatus Status { get; set; } = ResponseStatus.InProgress;

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 设备指纹
    /// </summary>
    [StringLength(100)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 元数据（JSON格式）
    /// </summary>
    [StringLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// 关联的问卷
    /// </summary>
    public virtual Survey Survey { get; set; } = null!;

    /// <summary>
    /// 回答详情集合
    /// </summary>
    public virtual ICollection<ResponseAnswer> Answers { get; set; } = new List<ResponseAnswer>();
}
