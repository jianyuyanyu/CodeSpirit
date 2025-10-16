using AutoMapper;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamSetting;
using CodeSpirit.ExamApi.Extensions;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试设置服务实现
/// </summary>
public class ExamSettingService : BaseCRUDService<ExamSetting, ExamSettingDto, long, CreateExamSettingDto, UpdateExamSettingDto>, IExamSettingService, IScopedDependency
{
    private readonly IRepository<ExamSetting> _repository;
    private readonly IRepository<ExamPaper> _examPaperRepository;
    private readonly IRepository<StudentGroup> _studentGroupRepository;
    private readonly IRepository<ExamSettingStudentGroup> _examSettingStudentGroupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ExamSettingService> _logger;
    private readonly ExamDbContext _context;
    private readonly IExamCacheService _examCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">考试设置仓储</param>
    /// <param name="examPaperRepository">试卷仓储</param>
    /// <param name="studentGroupRepository">学生组仓储</param>
    /// <param name="examSettingStudentGroupRepository">考试设置学生组仓储</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="examCacheService">考试缓存服务</param>
    public ExamSettingService(
        IRepository<ExamSetting> repository,
        IRepository<ExamPaper> examPaperRepository,
        IRepository<StudentGroup> studentGroupRepository,
        IRepository<ExamSettingStudentGroup> examSettingStudentGroupRepository,
        IMapper mapper,
        ILogger<ExamSettingService> logger,
        ExamDbContext context,
        IExamCacheService examCacheService)
        : base(repository, mapper)
    {
        _repository = repository;
        _examPaperRepository = examPaperRepository;
        _studentGroupRepository = studentGroupRepository;
        _examSettingStudentGroupRepository = examSettingStudentGroupRepository;
        _mapper = mapper;
        _logger = logger;
        _context = context;
        _examCacheService = examCacheService;
    }

    /// <summary>
    /// 获取考试设置分页列表
    /// </summary>
    public async Task<PageList<ExamSettingDto>> GetExamSettingsAsync(ExamSettingQueryDto queryDto)
    {
        var predicate = PredicateBuilder.New<ExamSetting>(true);

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(queryDto.Keywords))
        {
            predicate = predicate.And(x => x.Name.Contains(queryDto.Keywords) ||
                                         x.Description.Contains(queryDto.Keywords));
        }

        // 试卷ID筛选
        if (queryDto.ExamPaperId.HasValue)
        {
            predicate = predicate.And(x => x.ExamPaperId == queryDto.ExamPaperId.Value);
        }

        // 开始时间范围筛选
        if (queryDto.StartTimeFrom.HasValue)
        {
            predicate = predicate.And(x => x.StartTime >= queryDto.StartTimeFrom.Value);
        }
        if (queryDto.StartTimeTo.HasValue)
        {
            predicate = predicate.And(x => x.StartTime <= queryDto.StartTimeTo.Value);
        }

        // 结束时间范围筛选
        if (queryDto.EndTimeFrom.HasValue)
        {
            predicate = predicate.And(x => x.EndTime >= queryDto.EndTimeFrom.Value);
        }
        if (queryDto.EndTimeTo.HasValue)
        {
            predicate = predicate.And(x => x.EndTime <= queryDto.EndTimeTo.Value);
        }

        var query = _repository.CreateQuery()
            .Include(x => x.ExamPaper)
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .Include(x => x.ExamRecords) // 添加考试记录关联
            .Where(predicate);

        // 通过率范围筛选
        // 注意：由于通过率是计算得出的值，我们需要在内存中进行筛选
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // 计算并筛选通过率
        if (queryDto.MinPassRate.HasValue || queryDto.MaxPassRate.HasValue)
        {
            items = items.Where(x =>
            {
                if (!x.ExamRecords.Any()) return false;
                var passRate = (decimal)x.ExamRecords.Count(r => r.Score >= x.ExamPaper.PassScore) / x.ExamRecords.Count * 100;
                return (!queryDto.MinPassRate.HasValue || passRate >= queryDto.MinPassRate.Value) &&
                       (!queryDto.MaxPassRate.HasValue || passRate <= queryDto.MaxPassRate.Value);
            }).ToList();
        }

        // 计算总数并进行分页
        var total = items.Count;
        items = items
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToList();

        var dtos = _mapper.Map<List<ExamSettingDto>>(items);

        // 计算通过率信息
        foreach (var dto in dtos)
        {
            var examSetting = items.First(x => x.Id == dto.Id);
            var examRecords = examSetting.ExamRecords;
            var passScore = examSetting.ExamPaper.PassScore;
            
            dto.TotalParticipants = examRecords.Count;
            dto.PassedParticipants = examRecords.Count(r => r.Score >= passScore);
            dto.PassRate = dto.TotalParticipants > 0 
                ? Math.Round((decimal)dto.PassedParticipants / dto.TotalParticipants, 2)
                : null;
        }

        return new PageList<ExamSettingDto>(dtos, total);
    }

    /// <summary>
    /// 获取考试设置详情
    /// </summary>
    public async Task<ExamSettingDto> GetExamSettingDetailAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.ExamPaper)
            .Include(x => x.StudentGroups)
                .ThenInclude(x => x.StudentGroup)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            return null;
        }

        return _mapper.Map<ExamSettingDto>(examSetting);
    }

    /// <summary>
    /// 创建考试设置
    /// </summary>
    public override async Task<ExamSettingDto> CreateAsync(CreateExamSettingDto createDto)
    {
        // 验证试卷是否存在
        var examPaper = await _examPaperRepository.GetByIdAsync(createDto.ExamPaperId);
        if (examPaper == null)
        {
            throw new AppServiceException(404, "试卷不存在");
        }

        // 验证学生分组是否存在
        var studentGroups = await _studentGroupRepository.CreateQuery()
            .Where(x => createDto.StudentGroupIds.Contains(x.Id))
            .ToListAsync();

        if (studentGroups.Count != createDto.StudentGroupIds.Count)
        {
            throw new AppServiceException(404, "部分学生分组不存在");
        }

        // 创建考试设置
        var examSetting = _mapper.Map<ExamSetting>(createDto);
        examSetting.Status = ExamSettingStatus.Draft;
        await _repository.AddAsync(examSetting);

        // 创建考试设置-学生分组关联
        var examSettingStudentGroups = createDto.StudentGroupIds.Select(groupId => new ExamSettingStudentGroup
        {
            ExamSettingId = examSetting.Id,
            StudentGroupId = groupId
        }).ToList();

        await _examSettingStudentGroupRepository.AddRangeAsync(examSettingStudentGroups);

        return await GetExamSettingDetailAsync(examSetting.Id);
    }

    /// <summary>
    /// 更新考试设置
    /// </summary>
    public override async Task UpdateAsync(long id, UpdateExamSettingDto updateDto)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.StudentGroups)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以更新");
        }

        // 验证试卷是否存在
        var examPaper = await _examPaperRepository.GetByIdAsync(updateDto.ExamPaperId);
        if (examPaper == null)
        {
            throw new AppServiceException(404, "试卷不存在");
        }

        // 验证学生分组是否存在
        var studentGroups = await _studentGroupRepository.CreateQuery()
            .Where(x => updateDto.StudentGroupIds.Contains(x.Id))
            .ToListAsync();

        if (studentGroups.Count != updateDto.StudentGroupIds.Count)
        {
            throw new AppServiceException(404, "部分学生分组不存在");
        }

        // 更新基本信息
        _mapper.Map(updateDto, examSetting);

        // 更新学生分组关联
        await _repository.ExecuteInTransactionAsync(async () =>
        {
            // 删除原有关联
            foreach (var group in examSetting.StudentGroups)
            {
                await _examSettingStudentGroupRepository.DeleteAsync(group);
            }

            // 添加新关联
            var examSettingStudentGroups = updateDto.StudentGroupIds.Select(groupId => new ExamSettingStudentGroup
            {
                ExamSettingId = examSetting.Id,
                StudentGroupId = groupId
            }).ToList();

            await _examSettingStudentGroupRepository.AddRangeAsync(examSettingStudentGroups);
        });

        await _repository.UpdateAsync(examSetting);
        
        // 清理相关缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试设置更新后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试设置更新后的缓存失败: {ExamId}", id);
        }
    }

    /// <summary>
    /// 删除考试设置
    /// </summary>
    public override async Task DeleteAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.StudentGroups)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            return;
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以删除");
        }

        await _repository.DeleteAsync(examSetting);
        
        // 清理相关缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试设置删除后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试设置删除后的缓存失败: {ExamId}", id);
        }
    }

    /// <summary>
    /// 发布考试设置
    /// </summary>
    public async Task PublishExamSettingAsync(long id)
    {
        var examSetting = await _repository.GetByIdAsync(id);
        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Draft)
        {
            throw new AppServiceException(400, "只有草稿状态的考试设置可以发布");
        }

        // 检查试卷状态
        var examPaper = await _examPaperRepository.GetByIdAsync(examSetting.ExamPaperId);
        if (examPaper == null || examPaper.Status != ExamPaperStatus.Published)
        {
            throw new AppServiceException(400, "试卷未发布，不能发布考试设置");
        }

        //// 检查时间设置
        //if (examSetting.StartTime <= DateTime.UtcNow)
        //{
        //    throw new AppServiceException(400, "考试开始时间必须大于当前时间");
        //}

        if (examSetting.EndTime <= examSetting.StartTime)
        {
            throw new AppServiceException(400, "考试结束时间必须大于开始时间");
        }

        examSetting.Status = ExamSettingStatus.Published;
        await _repository.UpdateAsync(examSetting);
        
        // 发布时预热缓存（仅预热即将开始的考试）
        try
        {
            await _examCacheService.WarmupExamCacheAsync(id);
            _logger.LogDebug("已尝试预热考试发布后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预热考试发布后的缓存失败: {ExamId}", id);
        }
    }

    /// <summary>
    /// 取消发布考试设置
    /// </summary>
    public async Task UnpublishExamSettingAsync(long id)
    {
        var examSetting = await _repository.CreateQuery()
            .Include(x => x.ExamRecords)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (examSetting == null)
        {
            throw new AppServiceException(404, "考试设置不存在");
        }

        // 检查考试设置状态
        if (examSetting.Status != ExamSettingStatus.Published)
        {
            throw new AppServiceException(400, "只有已发布状态的考试设置可以取消发布");
        }

        // 检查是否有学生已经开始考试
        if (examSetting.ExamRecords.Any())
        {
            throw new AppServiceException(400, "已有学生开始考试，不能取消发布");
        }

        examSetting.Status = ExamSettingStatus.Draft;
        await _repository.UpdateAsync(examSetting);
        
        // 取消发布时清理缓存
        try
        {
            await _examCacheService.ClearExamCacheAsync(id);
            _logger.LogDebug("已清理考试取消发布后的缓存: {ExamId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理考试取消发布后的缓存失败: {ExamId}", id);
        }
    }

    /// <summary>
    /// 获取用户可参加的考试列表
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>可参加的考试列表</returns>
    public async Task<List<ClientExamDto>> GetAvailableExamsForClientAsync(long studentId)
    {
        try
        {
            // 查询当前用户所属的学生组
            var studentGroups = await _context.StudentGroupMappings
                .Where(m => m.StudentId == studentId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            // 获取可参加的考试
            var now = DateTime.UtcNow;
            // 定义预展示时间，开考前半小时（30分钟）可见
            var previewTime = now.AddMinutes(30);

            var availableExams = await _context.ExamSettings
                .Include(e => e.StudentGroups)
                .Include(e => e.ExamPaper)
                .Where(e => e.Status == ExamSettingStatus.Published)
                .Where(e =>
                    // 正在进行中的考试或即将开始的考试（开考前半小时）
                    ((e.StartTime <= now && e.EndTime >= now) ||
                     (e.StartTime > now && e.StartTime <= previewTime))
                )
                .Where(e => e.StudentGroups.Any() == false || e.StudentGroups.Any(g => studentGroups.Contains(g.StudentGroupId)))
                .Select(e => new ClientExamDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Duration = e.Duration,
                    TotalScore = e.ExamPaper.TotalScore,
                    Status = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == studentId &&
                        r.Status == ExamRecordStatus.InProgress)
                        ? "进行中"
                        : (_context.ExamRecords.Any(r =>
                            r.ExamSettingId == e.Id &&
                            r.StudentId == studentId &&
                            (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                            ? "已完成"
                            : (e.StartTime <= now && e.EndTime >= now ? "进行中" :
                               (e.StartTime > now ? "未开始" : "已结束"))),
                    // 检查是否已参加并获取成绩
                    HasResult = _context.ExamRecords.Any(r =>
                        r.ExamSettingId == e.Id &&
                        r.StudentId == studentId &&
                        (r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.InProgress))
                })
                .ToListAsync();

            return availableExams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可参加的考试列表时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试详情（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试详情</returns>
    public async Task<ClientExamDetailDto> GetExamDetailForClientAsync(long examId, long recordId)
    {
        try
        {
            // 获取考试基本信息
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new BusinessException("考试不存在");
            }

            // 获取考试记录（包含答题记录，其中保存了用户的题目顺序）
            var examRecord = await _context.ExamRecords
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new BusinessException("考试记录不存在");
            }
            
            // ⚠️ 如果AnswerRecords为空，可能是因为刚创建记录，需要重新加载
            if (examRecord.AnswerRecords == null || !examRecord.AnswerRecords.Any())
            {
                _logger.LogWarning("考试记录 {RecordId} 的答题记录为空，尝试重新加载", recordId);
                
                // 重新从数据库加载答题记录
                examRecord.AnswerRecords = await _context.ExamAnswerRecords
                    .Where(a => a.ExamRecordId == recordId)
                    .ToListAsync();
                    
                if (!examRecord.AnswerRecords.Any())
                {
                    _logger.LogError("考试记录 {RecordId} 没有答题记录，无法构建题目列表", recordId);
                    throw new BusinessException("考试记录没有答题记录");
                }
            }

            // 使用统一的缓存组件获取题目数据
            var questionDataDict = await _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);
            if (questionDataDict == null || !questionDataDict.Any())
            {
                _logger.LogError("无法获取考试 {ExamId} 的题目数据", examId);
                throw new BusinessException("无法获取考试题目数据");
            }

            // ✅ 使用已保存的 OrderNumber，而不是每次重新随机
            // 题目顺序在创建 ExamRecord 时已经确定并保存在 AnswerRecord.OrderNumber 中
            
            // 按照用户的 AnswerRecord.OrderNumber 排序构建题目列表，然后再按题型分组排序
            var questions = examRecord.AnswerRecords
                .OrderBy(a => a.OrderNumber)
                .Where(a => questionDataDict.ContainsKey(a.QuestionId))
                .Select(answerRecord =>
                {
                    var cachedQuestion = questionDataDict[answerRecord.QuestionId];
                    // 创建新对象，避免修改缓存中的数据
                    return new ClientExamQuestionDto
                    {
                        Id = cachedQuestion.Id,
                        QuestionId = cachedQuestion.QuestionId,
                        QuestionVersionId = cachedQuestion.QuestionVersionId,
                        Content = cachedQuestion.Content,
                        Type = cachedQuestion.Type,
                        Options = cachedQuestion.Options,
                        Score = cachedQuestion.Score,
                        IsRequired = cachedQuestion.IsRequired,
                        SequenceNumber = answerRecord.OrderNumber  // 设置用户特定的顺序号
                    };
                })
                .ToList();
            
            // 重新分配连续的序号
            for (int i = 0; i < questions.Count; i++)
            {
                questions[i].SequenceNumber = i + 1;
            }

            // 组装考试详情
            var examDetail = new ClientExamDetailDto
            {
                Id = examSetting.Id,
                RecordId = examRecord.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examRecord.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                AttemptNumber = examRecord.AttemptNumber,
                AllowedAttempts = examSetting.AllowedAttempts,
                Questions = questions
            };

            return examDetail;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "获取考试详情时发生错误");
            throw;
        }
    }


    /// <summary>
    /// 获取考试基本信息（客户端视图）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试基本信息</returns>
    public async Task<ClientExamBasicInfoDto> GetExamBasicInfoForClientAsync(long examId, long studentId, long? recordId = null)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 检查考试时间
            var now = DateTime.UtcNow;
            if (examSetting.StartTime > now || examSetting.EndTime < now)
            {
                throw new InvalidOperationException("不在考试时间范围内");
            }

            // 检查是否有权限参加考试
            var studentGroups = await _context.StudentGroupMappings
                .Where(m => m.StudentId == studentId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            var hasPermission = !examSetting.StudentGroups.Any() ||
                                examSetting.StudentGroups.Any(g => studentGroups.Contains(g.Id));

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 查找进行中的考试记录
            var existingRecord = recordId.HasValue
                ? await _context.ExamRecords
                    .Where(r => r.Id == recordId.Value)
                    .FirstOrDefaultAsync()
                : await _context.ExamRecords
                    .Where(r => r.ExamSettingId == examId &&
                          r.StudentId == studentId &&
                          r.Status == ExamRecordStatus.InProgress)
                    .OrderByDescending(r => r.StartTime)
                    .FirstOrDefaultAsync();

            // 组装考试基本信息
            var examBasicInfo = new ClientExamBasicInfoDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = existingRecord?.StartTime ?? now,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                RecordId = existingRecord?.Id,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                ScreenSwitchCount = existingRecord?.ScreenSwitchCount ?? 0,
                EnableViewResult = examSetting.EnableViewResult,
                MinExamTime = examSetting.MinExamTime
            };

            return examBasicInfo;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not InvalidOperationException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试基本信息时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试轻量信息（客户端视图，用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    public async Task<ClientExamLightInfoDto> GetExamLightInfoForClientAsync(long examId, long studentId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                throw new ArgumentException("考试不存在", nameof(examId));
            }

            // 检查是否有权限参加考试
            var studentGroups = await _context.StudentGroupMappings
                .Where(m => m.StudentId == studentId)
                .Select(m => m.StudentGroupId)
                .ToListAsync();

            var hasPermission = !examSetting.StudentGroups.Any() ||
                                examSetting.StudentGroups.Any(g => studentGroups.Contains(g.Id));

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 组装考试轻量信息
            var lightInfo = new ClientExamLightInfoDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examSetting.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                QuestionCount = examSetting.ExamPaper.ExamPaperQuestions.Count,
                ServerTime = DateTime.UtcNow,
                Status = string.Empty, // 将由ClientService设置
                CanStart = false // 将由ClientService设置
            };

            return lightInfo;
        }
        catch (Exception ex) when (
            ex is not ArgumentException &&
            ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "获取考试轻量信息时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 获取考试信息用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本配置信息，如果考试不存在则返回null</returns>
    public async Task<ExamBasicInfoCacheDto?> GetExamInfoForWarmupAsync(long examId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                return null;
            }

            // 只返回共享的考试配置信息，不包含用户特定的数据
            return new ExamBasicInfoCacheDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examSetting.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper.TotalScore,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                EnableViewResult = examSetting.EnableViewResult,
                MinExamTime = examSetting.MinExamTime,
                AllowedAttempts = examSetting.AllowedAttempts,
                EnableRandomQuestionOrder = examSetting.EnableRandomQuestionOrder,
                EnableRandomOptionOrder = examSetting.EnableRandomOptionOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试信息用于预热时发生错误，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试题目数据用于缓存预热（不进行学生权限验证）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典（QuestionId -> 题目详情），如果考试不存在则返回null</returns>
    public async Task<Dictionary<long, ClientExamQuestionDto>?> GetExamQuestionsForWarmupAsync(long examId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .ThenInclude(q => q.Question)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                return null;
            }

            // 加载题目版本信息
            foreach (var paperQuestion in examSetting.ExamPaper.ExamPaperQuestions)
            {
                await _context.Entry(paperQuestion)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
            }

            // 构建题目字典（QuestionId -> 题目详情）
            // 注意：这里只包含题目内容，不包含用户特定的顺序
            var questionsDict = examSetting.ExamPaper.ExamPaperQuestions
                .Where(q => q.Question != null && q.QuestionVersion != null)
                .ToDictionary(
                    q => q.QuestionId,
                    q => new ClientExamQuestionDto
                    {
                        Id = q.Id,
                        QuestionId = q.QuestionId,
                        QuestionVersionId = q.QuestionVersionId,
                        Content = q.QuestionVersion.Content,
                        Type = q.Question.Type.ToString(),
                        Options = q.QuestionVersion.Options
                            .Select(option => new OptionDisplayDto { Label = option, Value = option })
                            .ToList(),
                        Score = q.Score,
                        IsRequired = q.IsRequired,
                        // SequenceNumber 不在预热时设置，因为它是用户特定的
                        SequenceNumber = 0
                    }
                );

            _logger.LogDebug("成功获取考试题目数据用于预热，考试ID: {ExamId}, 题目数: {Count}", 
                examId, questionsDict.Count);

            return questionsDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试题目数据用于预热时发生错误，考试ID: {ExamId}", examId);
            throw;
        }
    }

}