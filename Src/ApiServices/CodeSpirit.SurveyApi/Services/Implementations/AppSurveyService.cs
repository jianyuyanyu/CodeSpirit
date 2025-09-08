using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.App;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// App端问卷服务实现
/// </summary>
public class AppSurveyService : IAppSurveyService, IScopedDependency
{
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AppSurveyService> _logger;

    /// <summary>
    /// 初始化App端问卷服务
    /// </summary>
    /// <param name="surveyRepository">问卷仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志记录器</param>
    public AppSurveyService(
        IRepository<Survey> surveyRepository,
        IMapper mapper,
        ILogger<AppSurveyService> logger)
    {
        ArgumentNullException.ThrowIfNull(surveyRepository);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(logger);

        _surveyRepository = surveyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取公开问卷列表
    /// </summary>
    /// <returns>公开问卷列表</returns>
    public async Task<List<AppSurveyDto>> GetPublicSurveysAsync()
    {
        return await GetPublicSurveysAsync(new AppSurveyQueryDto());
    }

    /// <summary>
    /// 获取公开问卷列表（带查询参数）
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>公开问卷列表</returns>
    public async Task<List<AppSurveyDto>> GetPublicSurveysAsync(AppSurveyQueryDto queryDto)
    {
        try
        {
            var query = _surveyRepository.Find(s => 
                s.Status == SurveyStatus.Published && 
                s.AccessType == SurveyAccessType.Public &&
                (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));

            // 标题模糊搜索
            if (!string.IsNullOrWhiteSpace(queryDto.Title))
            {
                query = query.Where(s => s.Title.Contains(queryDto.Title));
            }

            // 分类名称筛选
            if (!string.IsNullOrWhiteSpace(queryDto.CategoryName))
            {
                query = query.Where(s => s.Category != null && s.Category.Name == queryDto.CategoryName);
            }

            var surveys = await query
                .Include(s => s.Category)
                .Include(s => s.Questions)
                .OrderByDescending(s => s.PublishedAt)
                .ToListAsync();

            var result = _mapper.Map<List<AppSurveyDto>>(surveys);
            
            // 设置计算属性
            foreach (var item in result)
            {
                var survey = surveys.First(s => s.Id == item.Id);
                item.EstimatedMinutes = CalculateEstimatedMinutes(survey.Questions.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取公开问卷列表失败，查询参数: {@QueryDto}", queryDto);
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取问卷详情
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>问卷详情</returns>
    public async Task<AppSurveyDetailDto?> GetSurveyAsync(int id)
    {
        try
        {
            var survey = await _surveyRepository.Find(s => s.Id == id)
                .Include(s => s.Category)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync();

            if (survey == null)
            {
                return null;
            }

            // 检查问卷是否可以访问（仅允许公开问卷）
            if (survey.Status != SurveyStatus.Published || survey.AccessType != SurveyAccessType.Public)
            {
                throw new UnauthorizedAccessException("问卷不可访问");
            }

            // 检查问卷是否已过期
            if (survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException("问卷已过期");
            }

            var result = _mapper.Map<AppSurveyDetailDto>(survey);
            result.EstimatedMinutes = CalculateEstimatedMinutes(survey.Questions.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问卷详情失败，问卷ID: {SurveyId}", id);
            throw;
        }
    }

    /// <summary>
    /// 检查问卷是否可以参与
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>检查结果</returns>
    public async Task<object?> CheckSurveyAvailabilityAsync(int id)
    {
        try
        {
            var survey = await _surveyRepository.Find(s => s.Id == id)
                .FirstOrDefaultAsync();

            if (survey == null)
            {
                return null;
            }

            var result = new
            {
                Id = survey.Id,
                Title = survey.Title,
                Status = survey.Status.ToString(),
                AccessType = survey.AccessType.ToString(),
                IsPublished = survey.Status == SurveyStatus.Published,
                IsPublic = survey.AccessType == SurveyAccessType.Public,
                IsExpired = survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow,
                ExpiresAt = survey.ExpiresAt,
                CanParticipate = survey.Status == SurveyStatus.Published && 
                                survey.AccessType == SurveyAccessType.Public &&
                                (!survey.ExpiresAt.HasValue || survey.ExpiresAt.Value > DateTime.UtcNow)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查问卷可用性失败，问卷ID: {SurveyId}", id);
            throw;
        }
    }

    /// <summary>
    /// 获取问卷分类选项
    /// </summary>
    /// <returns>分类选项列表</returns>
    public async Task<List<object>> GetSurveyCategoriesAsync()
    {
        try
        {
            var categories = await _surveyRepository.Find(s => 
                s.Status == SurveyStatus.Published && 
                s.AccessType == SurveyAccessType.Public &&
                s.Category != null)
                .Include(s => s.Category)
                .Select(s => s.Category)
                .Distinct()
                .Where(c => c != null)
                .ToListAsync();

            var result = categories.Select(c => new { 
                label = c!.Name, 
                value = c.Name 
            }).ToList<object>();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问卷分类选项失败");
            throw;
        }
    }

    /// <summary>
    /// 计算预计完成时间（分钟）
    /// </summary>
    /// <param name="questionCount">题目数量</param>
    /// <returns>预计完成时间</returns>
    public int CalculateEstimatedMinutes(int questionCount)
    {
        // 基础时间：每题平均30秒，最少2分钟
        var estimatedMinutes = Math.Max(2, (int)Math.Ceiling(questionCount * 0.5));
        return estimatedMinutes;
    }

    /// <summary>
    /// 根据公开访问码获取问卷详情
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>问卷详情</returns>
    public async Task<AppSurveyDetailDto?> GetSurveyByAccessCodeAsync(string accessCode)
    {
        try
        {
            // 验证访问码格式
            if (!AccessCodeGenerator.IsValidAccessCode(accessCode))
            {
                _logger.LogWarning("无效的访问码格式: {AccessCode}", accessCode);
                return null;
            }

            var survey = await _surveyRepository.Find(s => s.PublicAccessCode == accessCode)
                .Include(s => s.Category)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync();

            if (survey == null)
            {
                _logger.LogWarning("未找到访问码对应的问卷: {AccessCode}", accessCode);
                return null;
            }

            // 检查问卷是否可以访问（仅允许公开问卷）
            if (survey.Status != SurveyStatus.Published || survey.AccessType != SurveyAccessType.Public)
            {
                _logger.LogWarning("问卷不可访问: {AccessCode}, Status: {Status}, AccessType: {AccessType}", 
                    accessCode, survey.Status, survey.AccessType);
                throw new UnauthorizedAccessException("问卷不可访问");
            }

            // 检查问卷是否已过期
            if (survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("问卷已过期: {AccessCode}, ExpiresAt: {ExpiresAt}", 
                    accessCode, survey.ExpiresAt);
                throw new InvalidOperationException("问卷已过期");
            }

            // 检查分享链接是否过期
            if (survey.ShareExpiresAt.HasValue && survey.ShareExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("分享链接已过期: {AccessCode}, ShareExpiresAt: {ShareExpiresAt}", 
                    accessCode, survey.ShareExpiresAt);
                throw new InvalidOperationException("分享链接已过期");
            }

            var result = _mapper.Map<AppSurveyDetailDto>(survey);
            result.EstimatedMinutes = CalculateEstimatedMinutes(survey.Questions.Count);

            _logger.LogInformation("成功获取问卷详情: {AccessCode}, SurveyId: {SurveyId}", 
                accessCode, survey.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据访问码获取问卷详情失败: {AccessCode}", accessCode);
            throw;
        }
    }

    /// <summary>
    /// 根据公开访问码检查问卷可用性
    /// </summary>
    /// <param name="accessCode">公开访问码</param>
    /// <returns>检查结果</returns>
    public async Task<object?> CheckSurveyAvailabilityByAccessCodeAsync(string accessCode)
    {
        try
        {
            // 验证访问码格式
            if (!AccessCodeGenerator.IsValidAccessCode(accessCode))
            {
                _logger.LogWarning("无效的访问码格式: {AccessCode}", accessCode);
                return null;
            }

            var survey = await _surveyRepository.Find(s => s.PublicAccessCode == accessCode)
                .FirstOrDefaultAsync();

            if (survey == null)
            {
                _logger.LogWarning("未找到访问码对应的问卷: {AccessCode}", accessCode);
                return null;
            }

            var isExpired = survey.ExpiresAt.HasValue && survey.ExpiresAt.Value < DateTime.UtcNow;
            var isShareExpired = survey.ShareExpiresAt.HasValue && survey.ShareExpiresAt.Value < DateTime.UtcNow;
            var isPublished = survey.Status == SurveyStatus.Published;
            var isPublic = survey.AccessType == SurveyAccessType.Public;
            var canParticipate = isPublished && isPublic && !isExpired && !isShareExpired;

            var result = new
            {
                Id = survey.Id,
                AccessCode = survey.PublicAccessCode,
                Title = survey.Title,
                Status = survey.Status.ToString(),
                AccessType = survey.AccessType.ToString(),
                IsPublished = isPublished,
                IsPublic = isPublic,
                IsExpired = isExpired,
                IsShareExpired = isShareExpired,
                ExpiresAt = survey.ExpiresAt,
                ShareExpiresAt = survey.ShareExpiresAt,
                CanParticipate = canParticipate
            };

            _logger.LogInformation("检查问卷可用性: {AccessCode}, CanParticipate: {CanParticipate}", 
                accessCode, canParticipate);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据访问码检查问卷可用性失败: {AccessCode}", accessCode);
            throw;
        }
    }
}
