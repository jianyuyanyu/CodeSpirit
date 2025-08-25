using CodeSpirit.Shared.Entities;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 问卷草稿实体
/// </summary>
public class SurveyDraft : AuditableEntityBase<int>
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// 会话ID（用于匿名用户标识）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（可为空，支持匿名用户）
    /// </summary>
    [StringLength(50)]
    public string? UserId { get; set; }

    /// <summary>
    /// 草稿数据（JSON格式）
    /// </summary>
    [Required]
    [StringLength(8000)]
    public string DraftData { get; set; } = string.Empty;

    /// <summary>
    /// 最后保存时间
    /// </summary>
    [Required]
    public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

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
    /// 关联的问卷
    /// </summary>
    public virtual Survey Survey { get; set; } = null!;
}
