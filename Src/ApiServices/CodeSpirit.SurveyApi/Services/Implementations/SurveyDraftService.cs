using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.Draft;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷草稿服务实现
/// </summary>
public class SurveyDraftService : ISurveyDraftService, IScopedDependency
{
    private readonly IRepository<SurveyDraft> _draftRepository;
    private readonly IRepository<Survey> _surveyRepository;
    private readonly ISurveySettingsService _settingsService;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyDraftService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="draftRepository">草稿仓储</param>
    /// <param name="surveyRepository">问卷仓储</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    public SurveyDraftService(
        IRepository<SurveyDraft> draftRepository,
        IRepository<Survey> surveyRepository,
        ISurveySettingsService settingsService,
        IMapper mapper,
        ILogger<SurveyDraftService> logger)
    {
        _draftRepository = draftRepository;
        _surveyRepository = surveyRepository;
        _settingsService = settingsService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 自动保存草稿
    /// </summary>
    /// <param name="request">保存请求</param>
    /// <returns>保存结果</returns>
    public async Task<SurveyDraftSaveResult> AutoSaveDraftAsync(AutoSaveDraftRequest request)
    {
        try
        {
            // 获取自动保存设置
            var autoSaveSettings = await _settingsService.GetAutoSaveSettingsAsync();

            // 检查是否启用自动保存
            if (!autoSaveSettings.Enabled)
            {
                return SurveyDraftSaveResult.CreateFailure("自动保存功能已禁用");
            }

            // 验证问卷是否存在
            var survey = await _surveyRepository.GetByIdAsync(request.SurveyId);
            if (survey == null)
            {
                return SurveyDraftSaveResult.CreateFailure("问卷不存在");
            }

            // 检查数据大小限制
            var dataSizeKB = System.Text.Encoding.UTF8.GetByteCount(request.DraftData) / 1024.0;
            if (dataSizeKB > autoSaveSettings.MaxDataSizeKB)
            {
                return SurveyDraftSaveResult.CreateFailure($"草稿数据大小超过限制（{autoSaveSettings.MaxDataSizeKB}KB）");
            }

            // 查找现有草稿
            var existingDrafts = _draftRepository.Find(d => d.SurveyId == request.SurveyId 
                && d.SessionId == request.SessionId
                && (string.IsNullOrEmpty(request.UserId) || d.UserId == request.UserId)).ToList();
            var existingDraft = existingDrafts.FirstOrDefault();

            var now = DateTime.UtcNow;
            var expiresAt = now.AddDays(autoSaveSettings.RetentionDays);

            if (existingDraft != null)
            {
                // 更新现有草稿
                existingDraft.DraftData = request.DraftData;
                existingDraft.LastSavedAt = now;
                existingDraft.ExpiresAt = expiresAt;
                existingDraft.IpAddress = request.IpAddress;
                existingDraft.UserAgent = request.UserAgent;

                await _draftRepository.UpdateAsync(existingDraft);

                _logger.LogDebug("更新草稿 {DraftId}，问卷 {SurveyId}，会话 {SessionId}", 
                    existingDraft.Id, request.SurveyId, request.SessionId);

                return SurveyDraftSaveResult.CreateSuccess(existingDraft.Id, now, expiresAt);
            }
            else
            {
                // 创建新草稿
                var newDraft = new SurveyDraft
                {
                    SurveyId = request.SurveyId,
                    SessionId = request.SessionId,
                    UserId = request.UserId,
                    DraftData = request.DraftData,
                    LastSavedAt = now,
                    ExpiresAt = expiresAt,
                    IpAddress = request.IpAddress,
                    UserAgent = request.UserAgent
                };

                var createdDraft = await _draftRepository.AddAsync(newDraft);

                _logger.LogDebug("创建草稿 {DraftId}，问卷 {SurveyId}，会话 {SessionId}", 
                    createdDraft.Id, request.SurveyId, request.SessionId);

                return SurveyDraftSaveResult.CreateSuccess(createdDraft.Id, now, expiresAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动保存草稿失败，问卷 {SurveyId}，会话 {SessionId}", 
                request.SurveyId, request.SessionId);
            return SurveyDraftSaveResult.CreateFailure("保存失败：" + ex.Message);
        }
    }

    /// <summary>
    /// 获取草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>草稿数据</returns>
    public Task<SurveyDraftDto?> GetDraftAsync(int surveyId, string sessionId, string? userId = null)
    {
        var drafts = _draftRepository.Find(d => d.SurveyId == surveyId && d.SessionId == sessionId
            && (string.IsNullOrEmpty(userId) || d.UserId == userId)
            && d.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(d => d.LastSavedAt)
            .ToList();

        var draft = drafts.FirstOrDefault();

        if (draft == null)
        {
            return Task.FromResult<SurveyDraftDto?>(null);
        }

        return Task.FromResult<SurveyDraftDto?>(_mapper.Map<SurveyDraftDto>(draft));
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    /// <param name="surveyId">问卷ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>异步任务</returns>
    public async Task DeleteDraftAsync(int surveyId, string sessionId, string? userId = null)
    {
        var drafts = _draftRepository.Find(d => d.SurveyId == surveyId && d.SessionId == sessionId
            && (string.IsNullOrEmpty(userId) || d.UserId == userId)).ToList();

        if (drafts.Any())
        {
            foreach (var draft in drafts)
            {
                await _draftRepository.DeleteAsync(draft);
            }
            _logger.LogDebug("删除草稿，问卷 {SurveyId}，会话 {SessionId}，用户 {UserId}", 
                surveyId, sessionId, userId);
        }
    }

    /// <summary>
    /// 清理过期草稿
    /// </summary>
    /// <returns>清理的草稿数量</returns>
    public async Task<int> CleanupExpiredDraftsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredDrafts = _draftRepository.Find(d => d.ExpiresAt <= now).ToList();

        if (expiredDrafts.Any())
        {
            foreach (var draft in expiredDrafts)
            {
                await _draftRepository.DeleteAsync(draft);
            }
            _logger.LogInformation("清理了 {Count} 个过期草稿", expiredDrafts.Count);
        }

        return expiredDrafts.Count;
    }

    /// <summary>
    /// 获取用户的所有草稿
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>草稿列表</returns>
    public Task<List<SurveyDraftDto>> GetUserDraftsAsync(string userId)
    {
        var drafts = _draftRepository.Find(d => d.UserId == userId && d.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(d => d.LastSavedAt)
            .ToList();

        return Task.FromResult(_mapper.Map<List<SurveyDraftDto>>(drafts));
    }

    /// <summary>
    /// 获取会话的所有草稿
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>草稿列表</returns>
    public Task<List<SurveyDraftDto>> GetSessionDraftsAsync(string sessionId)
    {
        var drafts = _draftRepository.Find(d => d.SessionId == sessionId && d.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(d => d.LastSavedAt)
            .ToList();

        return Task.FromResult(_mapper.Map<List<SurveyDraftDto>>(drafts));
    }
}
