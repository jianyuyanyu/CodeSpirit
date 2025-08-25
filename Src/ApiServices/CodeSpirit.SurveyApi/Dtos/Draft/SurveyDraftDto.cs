namespace CodeSpirit.SurveyApi.Dtos.Draft;

/// <summary>
/// 问卷草稿DTO
/// </summary>
[DisplayName("问卷草稿")]
public class SurveyDraftDto
{
    /// <summary>
    /// 草稿ID
    /// </summary>
    [DisplayName("草稿ID")]
    public int Id { get; set; }

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
    /// 用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public string? UserId { get; set; }

    /// <summary>
    /// 草稿数据
    /// </summary>
    [DisplayName("草稿数据")]
    public string DraftData { get; set; } = string.Empty;

    /// <summary>
    /// 最后保存时间
    /// </summary>
    [DisplayName("最后保存时间")]
    public DateTime LastSavedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime ExpiresAt { get; set; }
}
