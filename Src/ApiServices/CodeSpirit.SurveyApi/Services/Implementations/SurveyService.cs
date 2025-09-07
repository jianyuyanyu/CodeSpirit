using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.SurveyApi.Dtos.Survey;
using CodeSpirit.SurveyApi.Models;
using CodeSpirit.SurveyApi.Models.Enums;
using CodeSpirit.SurveyApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Utilities;
using LinqKit;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.SurveyApi.Services.Implementations;

/// <summary>
/// 问卷服务实现
/// </summary>
public class SurveyService : BaseCRUDService<Survey, SurveyDto, int, CreateSurveyDto, UpdateSurveyDto>, ISurveyService, IScopedDependency
{
    private readonly IRepository<Survey> _repository;
    private readonly IRepository<Question> _questionRepository;
    private readonly IRepository<SurveyResponse> _responseRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SurveyService> _logger;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">问卷仓储</param>
    /// <param name="questionRepository">题目仓储</param>
    /// <param name="responseRepository">回答仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志器</param>
    /// <param name="currentUser">当前用户</param>
    public SurveyService(
        IRepository<Survey> repository,
        IRepository<Question> questionRepository,
        IRepository<SurveyResponse> responseRepository,
        IMapper mapper,
        ILogger<SurveyService> logger,
        ICurrentUser currentUser)
        : base(repository, mapper)
    {
        _repository = repository;
        _questionRepository = questionRepository;
        _responseRepository = responseRepository;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取问卷分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷分页列表</returns>
    public async Task<PageList<SurveyDto>> GetSurveysAsync(SurveyQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<Survey>(true);

        // 关键词搜索
        if (!string.IsNullOrEmpty(queryDto.Title))
        {
            predicate = predicate.And(x => x.Title.Contains(queryDto.Title));
        }

        // 状态筛选
        if (queryDto.Status.HasValue)
        {
            predicate = predicate.And(x => x.Status == queryDto.Status.Value);
        }

        // 访问类型筛选
        if (queryDto.AccessType.HasValue)
        {
            predicate = predicate.And(x => x.AccessType == queryDto.AccessType.Value);
        }

        // 是否为模板筛选
        if (queryDto.IsTemplate.HasValue)
        {
            predicate = predicate.And(x => x.IsTemplate == queryDto.IsTemplate.Value);
        }

        // 分类筛选
        if (queryDto.CategoryId.HasValue)
        {
            predicate = predicate.And(x => x.CategoryId == queryDto.CategoryId.Value);
        }

        // 创建者筛选已移除，如需按创建者筛选请使用具体的业务方法

        // 时间范围筛选
        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt >= queryDto.StartTime.Value);
        }

        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt <= queryDto.EndTime.Value);
        }

        // 使用正确的仓储方法
        return await _repository.GetPagedAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy?.ToLower() switch
            {
                "title" => "Title",
                "status" => "Status", 
                "createdat" => "CreatedAt",
                "updatedat" => "UpdatedAt",
                _ => "UpdatedAt"
            },
            queryDto.OrderDir?.ToLower() == "desc" ? "desc" : "asc",
            "Questions", "Responses", "Category"
        ).ContinueWith(t => _mapper.Map<PageList<SurveyDto>>(t.Result));
    }

    /// <summary>
    /// 创建问卷（重写以自动生成访问码）
    /// </summary>
    /// <param name="createDto">创建问卷DTO</param>
    /// <returns>创建的问卷DTO</returns>
    public override async Task<SurveyDto> CreateAsync(CreateSurveyDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        // 创建问卷实体
        var survey = _mapper.Map<Survey>(createDto);
        
        // 自动生成访问码
        survey.PublicAccessCode = await GenerateUniqueAccessCodeAsync();
        
        // 设置租户ID
        survey.TenantId = _currentUser.TenantId ?? "default";

        // 保存问卷
        var createdSurvey = await _repository.AddAsync(survey);

        _logger.LogInformation("问卷创建成功：{Title}，ID：{SurveyId}，访问码：{AccessCode}，操作用户：{UserId}",
            createdSurvey.Title, createdSurvey.Id, createdSurvey.PublicAccessCode, _currentUser.Id);

        return _mapper.Map<SurveyDto>(createdSurvey);
    }

    /// <summary>
    /// 生成唯一的访问码
    /// </summary>
    /// <returns>唯一访问码</returns>
    private async Task<string> GenerateUniqueAccessCodeAsync()
    {
        string accessCode;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            accessCode = AccessCodeGenerator.GenerateSurveyAccessCode();
            attempts++;

            if (attempts > maxAttempts)
            {
                _logger.LogError("生成唯一访问码失败，超过最大尝试次数 {MaxAttempts}", maxAttempts);
                throw new InvalidOperationException("生成唯一访问码失败");
            }
        }
        while (await _repository.ExistsAsync(s => s.PublicAccessCode == accessCode));

        _logger.LogDebug("生成访问码成功：{AccessCode}，尝试次数：{Attempts}", accessCode, attempts);
        return accessCode;
    }

    /// <summary>
    /// 发布问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    public async Task PublishSurveyAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        if (survey.Status != SurveyStatus.Draft)
        {
            throw new BusinessException("只有草稿状态的问卷才能发布");
        }

        // 验证问卷是否有题目
        var hasQuestions = await _questionRepository.ExistsAsync(q => q.SurveyId == id);
        if (!hasQuestions)
        {
            throw new BusinessException("问卷必须包含至少一个题目才能发布");
        }

        // 验证问卷是否已预览
        if (!survey.IsPreviewChecked)
        {
            throw new BusinessException("问卷必须先预览后才能发布");
        }

        survey.Status = SurveyStatus.Published;
        survey.PublishedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(survey);

        _logger.LogInformation("问卷 {SurveyId} 已发布，操作用户：{UserId}", id, _currentUser.Id);
    }

    /// <summary>
    /// 关闭问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    public async Task CloseSurveyAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        if (survey.Status != SurveyStatus.Published)
        {
            throw new BusinessException("只有已发布的问卷才能关闭");
        }

        survey.Status = SurveyStatus.Closed;
        await _repository.UpdateAsync(survey);

        _logger.LogInformation("问卷 {SurveyId} 已关闭，操作用户：{UserId}", id, _currentUser.Id);
    }

    /// <summary>
    /// 归档问卷
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    public async Task ArchiveSurveyAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        if (survey.Status == SurveyStatus.Archived)
        {
            throw new BusinessException("问卷已经是归档状态");
        }

        survey.Status = SurveyStatus.Archived;
        await _repository.UpdateAsync(survey);

        _logger.LogInformation("问卷 {SurveyId} 已归档，操作用户：{UserId}", id, _currentUser.Id);
    }

    /// <summary>
    /// 复制问卷
    /// </summary>
    /// <param name="id">源问卷ID</param>
    /// <param name="title">新问卷标题</param>
    /// <returns>新问卷</returns>
    public async Task<SurveyDto> CopySurveyAsync(int id, string title)
    {
        var sourceSurvey = await _repository.GetByIdAsync(id);
        if (sourceSurvey == null)
        {
            throw new BusinessException("源问卷不存在");
        }

        // 创建新问卷
        var newSurvey = new Survey
        {
            Title = title,
            Description = sourceSurvey.Description,
            AccessType = sourceSurvey.AccessType,
            ExpiresAt = sourceSurvey.ExpiresAt,
            PublicAccessCode = await GenerateUniqueAccessCodeAsync(),
            IsTemplate = false,
            Status = SurveyStatus.Draft,
            TenantId = _currentUser.TenantId ?? "default"
        };

        var createdSurvey = await _repository.AddAsync(newSurvey);

        _logger.LogInformation("复制问卷 {SourceSurveyId} 为新问卷 {NewSurveyId}，操作用户：{UserId}", 
            id, createdSurvey.Id, _currentUser.Id);

        return _mapper.Map<SurveyDto>(createdSurvey);
    }

    /// <summary>
    /// 获取问卷统计信息
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>统计信息</returns>
    public async Task<SurveyStatisticsDto> GetSurveyStatisticsAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        var responses = _responseRepository.Find(r => r.SurveyId == id).ToList();

        var statistics = new SurveyStatisticsDto
        {
            SurveyId = id,
            Title = survey.Title,
            TotalResponses = responses.Count,
            CompletedResponses = responses.Count(r => r.Status == ResponseStatus.Completed),
            InProgressResponses = responses.Count(r => r.Status == ResponseStatus.InProgress),
            AbandonedResponses = responses.Count(r => r.Status == ResponseStatus.Abandoned)
        };

        // 计算完成率
        if (statistics.TotalResponses > 0)
        {
            statistics.CompletionRate = (decimal)statistics.CompletedResponses / statistics.TotalResponses * 100;
        }

        // 计算平均完成时间
        var completedResponses = responses.Where(r => r.Status == ResponseStatus.Completed && r.CompletedAt.HasValue);
        if (completedResponses.Any())
        {
            var averageMinutes = completedResponses
                .Select(r => (r.CompletedAt!.Value - r.StartedAt).TotalMinutes)
                .Average();
            statistics.AverageCompletionTimeMinutes = averageMinutes;
        }

        // 今日和本周新增
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        statistics.TodayNewResponses = responses.Count(r => r.StartedAt.Date == today);
        statistics.WeekNewResponses = responses.Count(r => r.StartedAt.Date >= weekStart);

        // 最近7天趋势
        statistics.Last7DaysTrend = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i))
            .Select(date => new DailyResponseCount
            {
                Date = date,
                Count = responses.Count(r => r.StartedAt.Date == date),
                CompletedCount = responses.Count(r => r.StartedAt.Date == date && r.Status == ResponseStatus.Completed)
            })
            .OrderBy(x => x.Date)
            .ToList();

        // 最后回答时间
        statistics.LastResponseAt = responses.OrderByDescending(r => r.StartedAt).FirstOrDefault()?.StartedAt;

        return statistics;
    }

    /// <summary>
    /// 获取我的问卷列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>问卷列表</returns>
    public async Task<PageList<SurveyDto>> GetMySurveysAsync(SurveyQueryDto queryDto)
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new BusinessException("用户未登录");
        }

        // 构建查询条件，专门针对当前用户的问卷
        var predicate = PredicateBuilder.New<Survey>(true);

        // 当前用户创建的问卷
        predicate = predicate.And(x => x.CreatedBy == _currentUser.Id.Value);

        // 其他查询条件
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Title.Contains(queryDto.Keywords) || 
                                          (x.Description != null && x.Description.Contains(queryDto.Keywords)));
        }

        if (!string.IsNullOrWhiteSpace(queryDto.Title))
        {
            predicate = predicate.And(x => x.Title.Contains(queryDto.Title));
        }

        if (queryDto.Status.HasValue)
        {
            predicate = predicate.And(x => x.Status == queryDto.Status.Value);
        }

        if (queryDto.AccessType.HasValue)
        {
            predicate = predicate.And(x => x.AccessType == queryDto.AccessType.Value);
        }

        if (queryDto.IsTemplate.HasValue)
        {
            predicate = predicate.And(x => x.IsTemplate == queryDto.IsTemplate.Value);
        }

        // 分类筛选
        if (queryDto.CategoryId.HasValue)
        {
            predicate = predicate.And(x => x.CategoryId == queryDto.CategoryId.Value);
        }

        if (queryDto.StartTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt >= queryDto.StartTime.Value);
        }

        if (queryDto.EndTime.HasValue)
        {
            predicate = predicate.And(x => x.CreatedAt <= queryDto.EndTime.Value);
        }

        return await _repository.GetPagedAsync(
            queryDto.Page,
            queryDto.PerPage,
            predicate,
            queryDto.OrderBy?.ToLower() switch
            {
                "title" => "Title",
                "status" => "Status", 
                "createdat" => "CreatedAt",
                "updatedat" => "UpdatedAt",
                _ => "UpdatedAt"
            },
            queryDto.OrderDir?.ToLower() == "desc" ? "desc" : "asc",
            "Questions", "Responses", "Category"
        ).ContinueWith(t => _mapper.Map<PageList<SurveyDto>>(t.Result));
    }

    /// <summary>
    /// 获取问卷模板列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>模板列表</returns>
    public async Task<PageList<SurveyDto>> GetSurveyTemplatesAsync(SurveyQueryDto queryDto)
    {
        queryDto.IsTemplate = true;
        queryDto.Status = SurveyStatus.Published;
        return await GetSurveysAsync(queryDto);
    }

    /// <summary>
    /// 从模板创建问卷
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <param name="title">新问卷标题</param>
    /// <returns>新问卷</returns>
    public async Task<SurveyDto> CreateFromTemplateAsync(int templateId, string title)
    {
        var template = await _repository.GetByIdAsync(templateId);
        if (template == null)
        {
            throw new BusinessException("模板不存在");
        }

        if (!template.IsTemplate)
        {
            throw new BusinessException("指定的问卷不是模板");
        }

        return await CopySurveyAsync(templateId, title);
    }

    /// <summary>
    /// 标记问卷已预览
    /// </summary>
    /// <param name="id">问卷ID</param>
    /// <returns>异步任务</returns>
    public async Task MarkPreviewedAsync(int id)
    {
        var survey = await _repository.GetByIdAsync(id);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        survey.IsPreviewChecked = true;
        await _repository.UpdateAsync(survey);
        
        _logger.LogInformation("问卷 {SurveyId} 已标记为已预览", id);
    }

    /// <summary>
    /// 获取问卷选项列表（用于下拉选择）
    /// </summary>
    /// <returns>问卷选项列表</returns>
    public async Task<List<SurveyOptionDto>> GetSurveyOptionsAsync()
    {
        var surveys = await _repository.Find(s => !s.IsTemplate)
            .OrderByDescending(s => s.CreatedAt)
            .Take(100) // 限制返回数量，避免数据过多
            .ToListAsync();

        return surveys.Select(s => new SurveyOptionDto
        {
            Id = s.Id,
            Title = s.Title,
            Status = s.Status.ToString()
        }).ToList();
    }

    /// <summary>
    /// 批量编辑问卷题目
    /// </summary>
    /// <param name="request">批量编辑请求</param>
    /// <returns>异步任务</returns>
    public async Task BatchEditQuestionsAsync(BatchEditQuestionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 验证问卷是否存在且有权限操作
        var survey = await _repository.GetByIdAsync(request.SurveyId);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        // 获取要编辑的题目
        var questionIds = request.Questions.Select(q => q.Id).ToList();
        var questions = await _questionRepository.Find(q => q.SurveyId == request.SurveyId && questionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count != request.Questions.Count)
        {
            throw new BusinessException("部分题目不存在或不属于该问卷");
        }

        // 应用批量编辑
        foreach (var questionItem in request.Questions)
        {
            var question = questions.First(q => q.Id == questionItem.Id);

            if (!string.IsNullOrEmpty(questionItem.Title))
            {
                question.Title = questionItem.Title;
            }

            if (questionItem.Description != null)
            {
                question.Description = questionItem.Description;
            }

            if (questionItem.IsRequired.HasValue)
            {
                question.IsRequired = questionItem.IsRequired.Value;
            }

            if (questionItem.OrderIndex.HasValue)
            {
                question.OrderIndex = questionItem.OrderIndex.Value;
            }

            if (questionItem.Validation != null)
            {
                question.Validation = questionItem.Validation;
            }

            if (questionItem.Settings != null)
            {
                question.Settings = questionItem.Settings;
            }

            await _questionRepository.UpdateAsync(question, false);
        }

        await _questionRepository.SaveChangesAsync();
        
        _logger.LogInformation("批量编辑了问卷 {SurveyId} 的 {Count} 个题目", request.SurveyId, request.Questions.Count);
    }

    /// <summary>
    /// 批量删除问卷题目
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <returns>异步任务</returns>
    public async Task BatchDeleteQuestionsAsync(BatchDeleteQuestionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 验证问卷是否存在且有权限操作
        var survey = await _repository.GetByIdAsync(request.SurveyId);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        // 验证问卷状态
        if (survey.Status == SurveyStatus.Published)
        {
            throw new BusinessException("已发布的问卷不能删除题目");
        }

        // 获取要删除的题目
        var questions = await _questionRepository.Find(q => q.SurveyId == request.SurveyId && request.QuestionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count != request.QuestionIds.Count)
        {
            throw new BusinessException("部分题目不存在或不属于该问卷");
        }

        // 删除题目
        foreach (var question in questions)
        {
            await _questionRepository.DeleteAsync(question.Id, false);
        }

        await _questionRepository.SaveChangesAsync();

        // 更新问卷题目数量（这里只是记录日志，实际的题目数量可以通过导航属性获取）
        var questionCount = await _questionRepository.Find(q => q.SurveyId == request.SurveyId).CountAsync();
        _logger.LogInformation("问卷 {SurveyId} 删除题目后，剩余题目数量：{QuestionCount}", request.SurveyId, questionCount);

        _logger.LogInformation("批量删除了问卷 {SurveyId} 的 {Count} 个题目", request.SurveyId, request.QuestionIds.Count);
    }

    /// <summary>
    /// 拖拽排序问卷题目
    /// </summary>
    /// <param name="request">排序请求</param>
    /// <returns>异步任务</returns>
    public async Task DragSortQuestionsAsync(DragSortQuestionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 验证问卷是否存在且有权限操作
        var survey = await _repository.GetByIdAsync(request.SurveyId);
        if (survey == null)
        {
            throw new BusinessException("问卷不存在");
        }

        // 获取所有题目
        var questionIds = request.Questions.Select(q => q.Id).ToList();
        var questions = await _questionRepository.Find(q => q.SurveyId == request.SurveyId && questionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count != request.Questions.Count)
        {
            throw new BusinessException("部分题目不存在或不属于该问卷");
        }

        // 更新排序
        foreach (var sortItem in request.Questions)
        {
            var question = questions.First(q => q.Id == sortItem.Id);
            question.OrderIndex = sortItem.OrderIndex;
            await _questionRepository.UpdateAsync(question, false);
        }

        await _questionRepository.SaveChangesAsync();
        
        _logger.LogInformation("更新了问卷 {SurveyId} 的题目排序", request.SurveyId);
    }
}
