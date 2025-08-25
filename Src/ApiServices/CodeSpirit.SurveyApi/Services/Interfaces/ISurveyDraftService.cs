using CodeSpirit.SurveyApi.Dtos.Draft;
using CodeSpirit.SurveyApi.Models;

namespace CodeSpirit.SurveyApi.Services.Interfaces;

/// <summary>
/// 问卷草稿服务接口
/// </summary>
public interface ISurveyDraftService
{
    /// <summary>
    /// 自动保存草稿
    /// </summary>
    /// <param name="request">保存请求</param>
    /// <returns>保存结果</returns>
    Task<SurveyDraftSaveResult> AutoSaveDraftAsync(AutoSaveDraftRequest request);

    /// <summary>
    /// 获取草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>草稿数据</returns>
    Task<SurveyDraftDto?> GetDraftAsync(int surveyId, string sessionId, string? userId = null);

    /// <summary>
    /// 删除草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>异步任务</returns>
    Task DeleteDraftAsync(int surveyId, string sessionId, string? userId = null);

    /// <summary>
    /// 清理过期草稿
    /// </summary>
    /// <returns>清理的草稿数量</returns>
    Task<int> CleanupExpiredDraftsAsync();

    /// <summary>
    /// 获取用户的所有草稿
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>草稿列表</returns>
    Task<List<SurveyDraftDto>> GetUserDraftsAsync(string userId);

    /// <summary>
    /// 获取会话的所有草稿
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>草稿列表</returns>
    Task<List<SurveyDraftDto>> GetSessionDraftsAsync(string sessionId);
}

/// <summary>
/// 自动保存草稿请求
/// </summary>
public class AutoSaveDraftRequest
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [Required]
    public int SurveyId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（可选）
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
    /// IP地址
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }
}

/// <summary>
/// 草稿保存结果
/// </summary>
public class SurveyDraftSaveResult
{
    /// <summary>
    /// 是否保存成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 草稿ID
    /// </summary>
    public int? DraftId { get; set; }

    /// <summary>
    /// 最后保存时间
    /// </summary>
    public DateTime? LastSavedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static SurveyDraftSaveResult CreateSuccess(int draftId, DateTime lastSavedAt, DateTime expiresAt)
    {
        return new SurveyDraftSaveResult
        {
            Success = true,
            DraftId = draftId,
            LastSavedAt = lastSavedAt,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static SurveyDraftSaveResult CreateFailure(string errorMessage)
    {
        return new SurveyDraftSaveResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
