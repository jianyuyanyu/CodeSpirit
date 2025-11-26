using AutoMapper;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Caching;
using CodeSpirit.ExamApi.Constants;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamPaper;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Settings.Services.Interfaces;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Services;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试记录服务实现类
/// </summary>
public class ExamRecordService : BaseCRUDService<ExamRecord, ExamRecordDto, long, StartExamDto, object>, IExamRecordService, IScopedDependency
{
    private const string ExamPaperExportSettings = "ExamPaperExportSettings";
    private readonly IRepository<ExamAnswerRecord> _answerRecordRepository;
    private readonly IRepository<ExamSetting> _examSettingRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<QuestionVersion> _questionVersionRepository;
    private readonly ILogger<ExamRecordService> _logger;
    private readonly IDistributedLockProvider _distributedLockProvider;
    private readonly ISettingsService _settingsService;
    private readonly IScoreConversionService _scoreConversionService;
    private readonly IExamCacheService _examCacheService;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">考试记录仓储</param>
    /// <param name="answerRecordRepository">答题记录仓储</param>
    /// <param name="examSettingRepository">考试设置仓储</param>
    /// <param name="studentRepository">学生仓储</param>
    /// <param name="questionVersionRepository">题目版本仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="distributedLockProvider">分布式锁提供者</param>
    /// <param name="settingsService">设置服务</param>
    /// <param name="scoreConversionService">分数转换服务</param>
    /// <param name="examCacheService">考试缓存服务</param>
    /// <param name="cacheService">缓存服务</param>
    public ExamRecordService(
        IRepository<ExamRecord> repository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<ExamSetting> examSettingRepository,
        IRepository<Student> studentRepository,
        IRepository<QuestionVersion> questionVersionRepository,
        IMapper mapper,
        ILogger<ExamRecordService> logger,
        IDistributedLockProvider distributedLockProvider,
        ISettingsService settingsService,
        IScoreConversionService scoreConversionService,
        IExamCacheService examCacheService,
        ICacheService cacheService) : base(repository, mapper)
    {
        _answerRecordRepository = answerRecordRepository;
        _examSettingRepository = examSettingRepository;
        _studentRepository = studentRepository;
        _questionVersionRepository = questionVersionRepository;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider ?? throw new ArgumentNullException(nameof(distributedLockProvider));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _scoreConversionService = scoreConversionService ?? throw new ArgumentNullException(nameof(scoreConversionService));
        _examCacheService = examCacheService ?? throw new ArgumentNullException(nameof(examCacheService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// 获取考试记录分页列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <param name="predicate">额外查询条件</param>
    /// <param name="includes">关联实体</param>
    /// <returns>考试记录分页列表</returns>
    public override async Task<PageList<ExamRecordDto>> GetPagedListAsync<TQueryDto>(
        TQueryDto queryDto,
        Expression<Func<ExamRecord, bool>>? predicate = null,
        params string[]? includes)
    {
        if (queryDto is ExamRecordQueryDto examRecordQueryDto)
        {
            var query = Repository.CreateQuery().AsNoTracking();

            // 合并传入的查询条件
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 按考试设置ID筛选
            if (examRecordQueryDto.ExamSettingId.HasValue)
            {
                query = query.Where(x => x.ExamSettingId == examRecordQueryDto.ExamSettingId.Value);
            }

            // 按学生姓名筛选（需要包含Student）
            if (!string.IsNullOrWhiteSpace(examRecordQueryDto.StudentName))
            {
                query = query.Where(x => x.Student.Name.Contains(examRecordQueryDto.StudentName));
            }

            // 按考试状态筛选
            if (examRecordQueryDto.Status.HasValue)
            {
                query = query.Where(x => x.Status == examRecordQueryDto.Status.Value);
            }

            // 按是否通过筛选
            if (examRecordQueryDto.IsPassed.HasValue)
            {
                query = query.Where(x => x.IsPassed == examRecordQueryDto.IsPassed.Value);
            }

            // 按开始时间范围筛选
            if (examRecordQueryDto.StartTimeRange != null && examRecordQueryDto.StartTimeRange.Length == 2)
            {
                DateTime startFrom = examRecordQueryDto.StartTimeRange[0];
                DateTime startTo = examRecordQueryDto.StartTimeRange[1].AddDays(1).AddSeconds(-1);
                query = query.Where(x => x.StartTime >= startFrom && x.StartTime <= startTo);
            }

            // 按提交时间范围筛选
            if (examRecordQueryDto.SubmitTimeRange != null && examRecordQueryDto.SubmitTimeRange.Length == 2)
            {
                DateTime submitFrom = examRecordQueryDto.SubmitTimeRange[0];
                DateTime submitTo = examRecordQueryDto.SubmitTimeRange[1].AddDays(1).AddSeconds(-1);
                query = query.Where(x => x.SubmitTime >= submitFrom && x.SubmitTime <= submitTo);
            }

            // 按作弊嫌疑等级最小值筛选
            if (examRecordQueryDto.MinCheatingSuspicionLevel.HasValue)
            {
                query = query.Where(x => x.CheatingSuspicionLevel >= examRecordQueryDto.MinCheatingSuspicionLevel.Value);
            }

            // 按身份证号码筛选
            if (!string.IsNullOrWhiteSpace(examRecordQueryDto.IdNo))
            {
                query = query.Where(x => x.Student.IdNo.Contains(examRecordQueryDto.IdNo));
            }

            // 🚀 性能优化：先获取总数（不加载关联数据）
            var totalCount = await query.CountAsync();

            // 🚀 性能优化：使用投影查询，在数据库层面计算各题型得分
            string orderBy = queryDto.OrderBy ?? "CreatedAt";
            string orderDir = queryDto.OrderDir ?? "desc";

            var examRecordDtos = await query
                .ApplySorting(orderBy, orderDir)
                .Skip((examRecordQueryDto.Page - 1) * examRecordQueryDto.PerPage)
                .Take(examRecordQueryDto.PerPage)
                .Select(x => new ExamRecordDto
                {
                    Id = x.Id,
                    ExamSettingId = x.ExamSettingId,
                    ExamName = x.ExamSetting.Name,
                    StudentId = x.StudentId,
                    StudentName = x.Student.Name,
                    AdmissionTicket = x.Student.AdmissionTicket,
                    IdNo = x.Student.IdNo,
                    AttemptNumber = x.AttemptNumber,
                    StartTime = x.StartTime,
                    SubmitTime = x.SubmitTime,
                    Status = x.Status,
                    Score = x.Score,
                    OriginalScore = x.OriginalScore,
                    IsScoreConverted = x.IsScoreConverted,
                    ScoreConversionRatio = x.ScoreConversionRatio,
                    // 🚀 在数据库层面计算各题型得分
                    SingleChoiceScore = x.AnswerRecords
                        .Where(a => a.QuestionVersion.Question.Type == QuestionType.SingleChoice && a.Score.HasValue)
                        .Sum(a => a.Score),
                    MultipleChoiceScore = x.AnswerRecords
                        .Where(a => a.QuestionVersion.Question.Type == QuestionType.MultipleChoice && a.Score.HasValue)
                        .Sum(a => a.Score),
                    TrueFalseScore = x.AnswerRecords
                        .Where(a => a.QuestionVersion.Question.Type == QuestionType.TrueFalse && a.Score.HasValue)
                        .Sum(a => a.Score),
                    IsPassed = x.IsPassed,
                    DeviceInfo = x.DeviceInfo,
                    ScreenSwitchCount = x.ScreenSwitchCount,
                    IpAddress = x.IpAddress,
                    CheatingSuspicionLevel = x.CheatingSuspicionLevel,
                    CheatingSuspicionRecord = x.CheatingSuspicionRecord,
                    Duration = x.Duration,
                    Comments = x.Comments ?? string.Empty,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PageList<ExamRecordDto>(examRecordDtos, totalCount);
        }

        return await base.GetPagedListAsync(queryDto, predicate, includes);
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    public async Task<ExamRecordDto> StartExamAsync(StartExamDto startExamDto)
    {
        // 验证考生是否存在
        if (!startExamDto.StudentId.HasValue)
        {
            throw new BusinessException("考生ID不能为空");
        }

        var student = await _studentRepository.GetByIdAsync(startExamDto.StudentId.Value);
        if (student == null)
        {
            throw new BusinessException("考生不存在");
        }

        // ✅ 统一使用CreateExamRecordAsync创建考试记录和答题记录
        try
        {
            var examRecord = await CreateExamRecordAsync(
                startExamDto.ExamSettingId,
                startExamDto.StudentId.Value,
                startExamDto.IpAddress ?? string.Empty,
                startExamDto.DeviceInfo ?? string.Empty);

            return Mapper.Map<ExamRecordDto>(examRecord);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message);
        }
    }

    /// <summary>
    /// 批量提交答案
    /// </summary>
    /// <param name="examRecordId">考试记录ID</param>
    /// <param name="answers">答案列表</param>
    /// <returns>是否全部成功</returns>
    public async Task<bool> SubmitAnswersAsync(long examRecordId, List<ClientExamAnswerDto> answers)
    {
        if (answers == null || !answers.Any())
        {
            throw new ArgumentException("答案列表不能为空", nameof(answers));
        }

        // ⚡⚡⚡ 极致性能优化：使用缓存验证考试记录状态，无需查询数据库
        var cachedExamRecord = await _examCacheService.GetExamRecordWithCacheAsync(examRecordId);
        
        if (cachedExamRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        // 检查考试状态
        if (cachedExamRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束，无法提交答案");
        }

        // ⚡ 性能优化：使用缓存获取试卷题目映射，避免数据库查询
        var questionDataDict = await _examCacheService.GetExamQuestionsDataWithCacheAsync(cachedExamRecord.ExamSettingId);
        
        if (questionDataDict == null || !questionDataDict.Any())
        {
            throw new BusinessException("试卷信息不存在，无法提交答案");
        }

        // 提取需要的题目ID列表
        var submittedQuestionIds = answers.Select(a => a.QuestionId).ToList();

        // ⚡⚡⚡ 极致性能优化：只查询需要更新的答题记录（无法完全缓存，因为需要跟踪实体以便更新）
        var existingAnswerRecords = await _answerRecordRepository.CreateQuery()
            .Where(a => a.ExamRecordId == examRecordId && submittedQuestionIds.Contains(a.QuestionId))
            .ToListAsync();

        var existingAnswerMap = existingAnswerRecords
            .ToDictionary(a => a.QuestionId, a => a);

        List<ExamAnswerRecord> recordsToUpdate = new List<ExamAnswerRecord>();
        List<ExamAnswerRecord> recordsToAdd = new List<ExamAnswerRecord>();
        var now = DateTime.UtcNow;

        foreach (var answer in answers)
        {
            // ⚡ 性能优化：使用缓存中的题目数据验证
            if (!questionDataDict.TryGetValue(answer.QuestionId, out var questionData))
            {
                _logger.LogWarning("题目 {QuestionId} 不在试卷中，已跳过", answer.QuestionId);
                continue;
            }

            // 查找现有的答题记录
            if (existingAnswerMap.TryGetValue(answer.QuestionId, out var answerRecord))
            {
                // 更新已有记录
                answerRecord.Answer = answer.Answer;
                answerRecord.SubmitTime = now;

                // 计算答题用时
                if (answerRecord.StartTime.HasValue)
                {
                    answerRecord.Duration = (int)(now - answerRecord.StartTime.Value).TotalSeconds;
                }

                recordsToUpdate.Add(answerRecord);
            }
            else
            {
                // ⚠️ 严重错误：答题记录应该在CreateExamRecordAsync时已全部创建
                // 如果走到这里，说明数据不一致，不能简单补充创建，因为：
                // 1. 无法获取正确的OrderNumber（可能经过乱序处理）
                // 2. 补充创建会导致题目顺序错乱
                // 3. 可能存在其他数据完整性问题
                _logger.LogError("数据异常：答题记录不存在，无法提交答案。ExamRecordId={ExamRecordId}, QuestionId={QuestionId}", 
                    examRecordId, answer.QuestionId);
                throw new BusinessException($"答题记录数据异常（题目ID: {answer.QuestionId}），请重新开始考试");
            }
        }

        try
        {
            // 批量保存更改
            if (recordsToUpdate.Any())
            {
                await _answerRecordRepository.UpdateRangeAsync(recordsToUpdate);
            }

            if (recordsToAdd.Any())
            {
                await _answerRecordRepository.AddRangeAsync(recordsToAdd);
            }

            // 记录日志
            _logger.LogDebug("批量答案提交成功。考试记录ID: {ExamRecordId}, 更新: {UpdateCount}, 新增: {AddCount}", 
                examRecordId, recordsToUpdate.Count, recordsToAdd.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量答案保存失败。考试记录ID: {ExamRecordId}", examRecordId);
            throw new BusinessException("答案保存失败，请重试");
        }
    }

    /// <summary>
    /// 完成考试
    /// </summary>
    public async Task<ExamRecordDto> FinishExamAsync(FinishExamDto finishExamDto)
    {
        // 验证考试记录是否存在
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.AnswerRecords)
            .Include(r => r.ExamSetting)
            .ThenInclude(es => es.ExamPaper)
            .FirstOrDefaultAsync(r => r.Id == finishExamDto.ExamRecordId);

        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        // 检查考试状态
        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束");
        }

        // 检查是否所有题目都已作答
        var unansweredQuestions = examRecord.AnswerRecords.Count(a => string.IsNullOrEmpty(a.Answer));
        if (unansweredQuestions > 0 && !finishExamDto.ForceSubmit)
        {
            throw new BusinessException($"还有{unansweredQuestions}道题目未作答，是否确认提交？");
        }

        // 更新考试记录状态
        examRecord.Status = ExamRecordStatus.Submitted;
        examRecord.SubmitTime = finishExamDto.SubmitTime ?? DateTime.UtcNow;

        if (examRecord.StartTime != null && examRecord.SubmitTime != null)
        {
            examRecord.Duration = (int)(examRecord.SubmitTime.Value - examRecord.StartTime).TotalMinutes;
        }

        // 自动评分（客观题）
        double totalScore = 0;
        foreach (var answer in examRecord.AnswerRecords)
        {
            var questionVersion = await _questionVersionRepository.CreateQuery()
                .Include(qv => qv.Question)
                .FirstOrDefaultAsync(qv => qv.Id == answer.QuestionVersionId);

            if (questionVersion != null && !string.IsNullOrEmpty(answer.Answer))
            {
                // 判断题和单选题可以自动评分
                if (questionVersion.Question.Type == QuestionType.SingleChoice ||
                    questionVersion.Question.Type == QuestionType.TrueFalse)
                {
                    bool isCorrect = answer.Answer.Trim() == questionVersion.CorrectAnswer.Trim();
                    answer.IsCorrect = isCorrect;
                    answer.Score = isCorrect ? questionVersion.DefaultScore : 0;
                    totalScore += answer.Score ?? 0;
                }
                // 多选题也可以自动评分，但需要特殊处理
                else if (questionVersion.Question.Type == QuestionType.MultipleChoice)
                {
                    var studentAnswers = answer.Answer.Split(',').Select(a => a.Trim()).OrderBy(a => a).ToArray();
                    var correctAnswers = questionVersion.CorrectAnswer.Split(',').Select(a => a.Trim()).OrderBy(a => a).ToArray();

                    bool isCorrect = studentAnswers.SequenceEqual(correctAnswers);
                    answer.IsCorrect = isCorrect;
                    answer.Score = isCorrect ? questionVersion.DefaultScore : 0;
                    totalScore += answer.Score ?? 0;
                }
                // 其他题型需要人工评分
            }
        }

        // 应用成绩换算
        await ApplyScoreConversion(examRecord, totalScore);

        // 保存更改
        await _answerRecordRepository.UpdateRangeAsync(examRecord.AnswerRecords);
        await Repository.UpdateAsync(examRecord);

        return Mapper.Map<ExamRecordDto>(examRecord);
    }

    /// <summary>
    /// 应用成绩换算逻辑
    /// </summary>
    /// <param name="examRecord">考试记录</param>
    /// <param name="originalScore">原始成绩</param>
    private async Task ApplyScoreConversion(ExamRecord examRecord, double originalScore)
    {
        var examPaper = examRecord.ExamSetting.ExamPaper;

        // 保存原始成绩
        examRecord.OriginalScore = originalScore;

        if (!examPaper.EnableScoreConversion ||
            examPaper.OriginalTotalScore == null ||
            examPaper.ConversionRatio == null)
        {
            // 未启用换算，原始成绩即为最终成绩
            examRecord.Score = originalScore;
            examRecord.IsScoreConverted = false;
            examRecord.ScoreConversionRatio = null;
        }
        else
        {
            // 应用换算规则
            var convertedScore = _scoreConversionService.ConvertScore(
                originalScore,
                examPaper.ConversionRatio.Value,
                examPaper.ConversionDecimalPlaces);

            examRecord.Score = (double)convertedScore; // Score字段存储最终显示成绩（换算后）
            examRecord.IsScoreConverted = true;
            examRecord.ScoreConversionRatio = examPaper.ConversionRatio;
        }

        // 计算是否通过（基于PassScore字段，无需额外计算）
        // PassScore字段已经根据是否启用换算设置为正确的及格分
        examRecord.IsPassed = examRecord.Score >= examPaper.PassScore;
    }

    /// <summary>
    /// 获取考试统计
    /// </summary>
    public async Task<ExamStatisticsDto> GetExamStatisticsAsync(long examSettingId)
    {
        var examSetting = await _examSettingRepository.GetByIdAsync(examSettingId);
        if (examSetting == null)
        {
            throw new BusinessException("考试设置不存在");
        }

        // 查询考试记录数据
        var records = await Repository.CreateQuery()
            .Where(r => r.ExamSettingId == examSettingId)
            .ToListAsync();

        if (records.Count == 0)
        {
            return new ExamStatisticsDto
            {
                ExamSettingId = examSettingId,
                ExamName = examSetting.Name
            };
        }

        // 计算统计数据
        int totalParticipants = records.Select(r => r.StudentId).Distinct().Count();
        int completedCount = records.Count(r => r.Status == ExamRecordStatus.Submitted || r.Status == ExamRecordStatus.Graded);
        int passedCount = records.Count(r => r.IsPassed);
        decimal passRate = completedCount > 0 ? (decimal)passedCount / completedCount * 100 : 0;

        var scoredRecords = records.Where(r => r.Score.HasValue).ToList();
        decimal averageScore = scoredRecords.Count > 0 ? (decimal)scoredRecords.Average(r => r.Score.Value) : 0;
        double highestScore = scoredRecords.Count > 0 ? scoredRecords.Max(r => r.Score.Value) : 0;
        double lowestScore = scoredRecords.Count > 0 ? scoredRecords.Min(r => r.Score.Value) : 0;

        var completedRecords = records.Where(r => r.Duration.HasValue).ToList();
        double averageTime = completedRecords.Count > 0 ? completedRecords.Average(r => r.Duration.Value) : 0;

        int cheatingSuspicionCount = records.Count(r => r.CheatingSuspicionLevel >= 50);

        return new ExamStatisticsDto
        {
            ExamSettingId = examSettingId,
            ExamName = examSetting.Name,
            TotalParticipants = totalParticipants,
            CompletedCount = completedCount,
            PassedCount = passedCount,
            PassRate = passRate,
            AverageScore = averageScore,
            HighestScore = highestScore,
            LowestScore = lowestScore,
            AverageCompletionTime = averageTime,
            CheatingSuspicionCount = cheatingSuspicionCount
        };
    }

    /// <summary>
    /// 获取错题列表
    /// </summary>
    public async Task<PageList<WrongQuestionDto>> GetWrongQuestionsAsync(WrongQuestionQueryDto queryDto)
    {
        // 构建查询条件
        Expression<Func<ExamAnswerRecord, bool>> predicate = a =>
            a.IsCorrect == false &&
            (!queryDto.StudentId.HasValue || a.ExamRecord.StudentId == queryDto.StudentId.Value) &&
            (!queryDto.ExamSettingId.HasValue || a.ExamRecord.ExamSettingId == queryDto.ExamSettingId.Value) &&
            (!queryDto.QuestionId.HasValue || a.QuestionId == queryDto.QuestionId.Value);

        // 查询错题记录
        var query = _answerRecordRepository.CreateQuery()
            .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
            .Where(predicate);

        // 按时间范围筛选
        if (queryDto.ExamTimeRange != null && queryDto.ExamTimeRange.Length == 2)
        {
            query = query.Where(a => a.ExamRecord.StartTime >= queryDto.ExamTimeRange[0] &&
                                  a.ExamRecord.StartTime <= queryDto.ExamTimeRange[1]);
        }

        // 按题目类型筛选
        if (!string.IsNullOrEmpty(queryDto.QuestionType))
        {
            if (Enum.TryParse<QuestionType>(queryDto.QuestionType, out var questionType))
            {
                query = query.Where(a => a.QuestionVersion.Question.Type == questionType);
            }
        }

        // 包含关联数据（需要在筛选之后加载）
        query = query.Include(a => a.ExamRecord)
                     .Include(a => a.QuestionVersion)
                     .Include(a => a.QuestionVersion.Question);

        // 排序和分页
        string orderBy = queryDto.OrderBy ?? "ExamRecord.StartTime";
        string orderDir = queryDto.OrderDir ?? "desc";

        int totalCount = await query.CountAsync();

        var pagedItems = await query
            .ApplySorting(orderBy, orderDir)
            .Skip((queryDto.Page - 1) * queryDto.PerPage)
            .Take(queryDto.PerPage)
            .ToListAsync();

        // 映射为DTO
        var wrongQuestionDtos = Mapper.Map<List<WrongQuestionDto>>(pagedItems);

        return new PageList<WrongQuestionDto>(wrongQuestionDtos, totalCount);
    }

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    public async Task<bool> RecordScreenSwitchAsync(long recordId)
    {
        var examRecord = await Repository.GetByIdAsync(recordId);
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        if (examRecord.Status != ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试已结束，无法记录切屏");
        }

        // 增加切屏记录
        examRecord.ScreenSwitchCount++;

        // 根据切屏次数判断作弊嫌疑
        var examSetting = await _examSettingRepository.GetByIdAsync(examRecord.ExamSettingId);
        if (examSetting != null)
        {
            if (examRecord.ScreenSwitchCount > examSetting.AllowedScreenSwitchCount)
            {
                // 计算作弊嫌疑等级
                int suspicionLevel = Math.Min(100, 50 + (examRecord.ScreenSwitchCount - examSetting.AllowedScreenSwitchCount) * 10);
                examRecord.CheatingSuspicionLevel = suspicionLevel;

                // 记录作弊日志
                var cheatingSuspicionRecord = examRecord.CheatingSuspicionRecord ?? "[]";
                var cheatRecord = $"{{\"time\":\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\"type\":\"screen_switch\",\"count\":{examRecord.ScreenSwitchCount}}}";
                examRecord.CheatingSuspicionRecord = cheatingSuspicionRecord.TrimEnd(']') + (cheatingSuspicionRecord.Length > 2 ? "," : "") + cheatRecord + "]";
            }
        }

        await Repository.UpdateAsync(examRecord);

        return true;
    }

    /// <summary>
    /// 获取考试记录及答题详情
    /// </summary>
    public async Task<ExamRecordDto> GetExamRecordDetailAsync(long recordId)
    {
        var examRecord = await Repository.CreateQuery()
            .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
            .Include(r => r.ExamSetting)
            .Include(r => r.Student)
            .Include(r => r.AnswerRecords)
            .ThenInclude(a => a.QuestionVersion)
            .ThenInclude(qv => qv.Question)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        return Mapper.Map<ExamRecordDto>(examRecord);
    }

    /// <summary>
    /// 获取答题预览要素
    /// </summary>
    /// <param name="recordId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<AnswerPreviewDto> GetAnswerPreviewAsync(long recordId)
    {
        var examRecord = await Repository.CreateQuery()
            .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
            .Include(r => r.ExamSetting)
            .ThenInclude(es => es.ExamPaper)
            .Include(r => r.AnswerRecords)
            .ThenInclude(a => a.QuestionVersion)
            .Include(r => r.AnswerRecords)
            .ThenInclude(a => a.Question)
            .Where(r => r.Id == recordId)
            .Select(x => new AnswerPreviewDto
            {
                ExamPaperId = x.ExamSetting.ExamPaperId,
                TotalScore = x.ExamSetting.ExamPaper.TotalScore,
                StudentScore = x.Score,
                Answers = x.AnswerRecords == null ? new List<ClientExamAnswerWithCorrectDto>() : x.AnswerRecords.Select(a => new ClientExamAnswerWithCorrectDto
                {
                    QuestionId = a.QuestionId,
                    Answer = a.Answer,
                    CorrectAnswer = a.QuestionVersion.CorrectAnswer,
                    QuestionType = a.Question.Type.ToString(),
                    Score = a.Score,
                    IsCorrect = a.IsCorrect,
                    DefaultScore = a.QuestionVersion.DefaultScore
                }).ToList()
            }).FirstOrDefaultAsync();
        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        return examRecord;
    }

    /// <summary>
    /// 创建考试记录和答题记录（统一入口）
    /// </summary>
    /// <remarks>
    /// ⚠️ 这是创建考试记录的唯一入口方法，包含以下功能：
    /// 1. 使用分布式锁防止并发创建
    /// 2. 验证考试设置和考生信息
    /// 3. 检查考试时间范围
    /// 4. 检查考试次数限制
    /// 5. 创建考试记录（ExamRecord）
    /// 6. 创建答题记录（AnswerRecords），包含题目顺序
    /// 7. 支持题目乱序
    /// 
    /// 其他方法（如StartExamAsync）应该调用此方法，而不是重复实现逻辑
    /// </remarks>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <returns>考试记录</returns>
    /// <exception cref="ArgumentException">考试不存在</exception>
    /// <exception cref="InvalidOperationException">不在考试时间范围内、已超过考试次数等</exception>
    /// <exception cref="AppServiceException">系统繁忙（获取锁超时）</exception>
    public async Task<ExamRecord> CreateExamRecordAsync(long examId, long studentId, string userIp, string deviceInfo)
    {
        // 使用分布式锁确保同一用户同一考试在同一时间只能创建一次记录
        var lockKey = $"exam_create_{examId}_{studentId}";

        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10)))
            {
                _logger.LogInformation("已获取考试创建锁: {LockKey}", lockKey);

                // 使用缓存获取考试基本信息
                var examBasicInfo = await _examCacheService.GetExamBasicInfoWithCacheAsync(examId);

                if (examBasicInfo == null)
                {
                    throw new BusinessException("考试不存在");
                }

                // 检查考试时间
                var now = DateTime.UtcNow;
                if (examBasicInfo.StartTime > now || examBasicInfo.EndTime < now)
                {
                    throw new BusinessException("不在考试时间范围内");
                }

                // 查找是否已存在未完成的考试记录（在锁内再次检查，确保并发安全）
                var existingRecord = await Repository.CreateQuery()
                    .Where(r => r.ExamSettingId == examId &&
                            r.StudentId == studentId &&
                            r.Status == ExamRecordStatus.InProgress)
                    .FirstOrDefaultAsync();

                // 如果存在进行中的考试记录，验证是否有答题记录
                if (existingRecord != null)
                {
                    // 检查该考试记录是否有答题记录
                    var answerRecordCount = await _answerRecordRepository.CreateQuery()
                        .Where(ar => ar.ExamRecordId == existingRecord.Id)
                        .CountAsync();

                    if (answerRecordCount > 0)
                    {
                        _logger.LogInformation("用户已有进行中的考试记录，直接返回: 考试ID={ExamId}, 学生ID={StudentId}, 记录ID={RecordId}, 答题记录数={AnswerCount}",
                            examId, studentId, existingRecord.Id, answerRecordCount);
                        return existingRecord;
                    }
                    else
                    {
                        // 存在考试记录但没有答题记录，这是异常情况，删除该记录并重新创建
                        _logger.LogWarning("发现异常的考试记录（无答题记录），将删除并重新创建: 考试ID={ExamId}, 学生ID={StudentId}, 记录ID={RecordId}",
                            examId, studentId, existingRecord.Id);
                        await Repository.DeleteAsync(existingRecord);
                    }
                }

                // ✅ 从缓存中获取已完成的考试次数（避免数据库查询）
                var completedCountCacheKey = new ExamCacheOptions.CompletedExamCount(examId, studentId);
                int? completedCountValue = await _cacheService.GetAsync<int>(completedCountCacheKey.Key);
                var completedCount = completedCountValue ?? 0;

                if (completedCount > 0)
                {
                    // ✅ 检查是否超过允许的考试次数
                    if (examBasicInfo.AllowedAttempts > 0 && completedCount >= examBasicInfo.AllowedAttempts)
                    {
                        _logger.LogInformation("考生 {StudentId} 已超过允许的考试次数，考试ID: {ExamId}，已完成次数: {CompletedCount}，允许考试次数: {AllowedAttempts}",
                            studentId, examId, completedCount, examBasicInfo.AllowedAttempts);
                        throw new BusinessException($"您已完成该考试的 {completedCount} 次，已达到允许的最大次数 {examBasicInfo.AllowedAttempts} 次，无法重新开始考试");
                    }

                    // ✅ 如果允许重新开始，记录日志
                    _logger.LogInformation("考生 {StudentId} 已有已提交的考试记录，但未超过允许次数，允许重新开始: 考试ID={ExamId}，已完成次数={CompletedCount}，允许次数={AllowedAttempts}",
                        studentId, examId, completedCount, examBasicInfo.AllowedAttempts);
                }

                // ✅ 计算新的尝试次数
                // 如果没有进行中的记录，根据已完成的次数计算新的尝试次数
                int nextAttemptNumber = 1;
                if (completedCount > 0)
                {
                    // 如果存在已完成的记录，新的尝试次数 = 已完成次数 + 1
                    nextAttemptNumber = completedCount + 1;
                }
                else if (existingRecord != null)
                {
                    // 如果存在进行中的记录（但没有答题记录，将被删除），使用其尝试次数
                    nextAttemptNumber = existingRecord.AttemptNumber;
                }

                // ✅ 使用数据库事务确保考试记录和答题记录的原子性
                // 分布式锁 + 数据库事务：双重保障数据一致性
                ExamRecord? examRecord = null;

                await Repository.ExecuteInTransactionAsync(async () =>
                {
                    // 创建考试记录
                    examRecord = new ExamRecord
                    {
                        ExamSettingId = examId,
                        StudentId = studentId,
                        AttemptNumber = nextAttemptNumber,
                        StartTime = now,
                        Status = ExamRecordStatus.InProgress,
                        IpAddress = userIp,
                        DeviceInfo = deviceInfo
                    };

                    await Repository.AddAsync(examRecord);

                    // ✅ 创建答题记录（使用缓存获取题目数据）
                    var questionsData = await _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);

                    if (questionsData == null || !questionsData.Any())
                    {
                        _logger.LogError("考试 {ExamId} 没有试卷题目，无法创建答题记录", examId);
                        throw new BusinessException("试卷题目不存在");
                    }

                    var questionsList = questionsData.Values.ToList();

                    // 题目乱序处理
                    if (examBasicInfo.EnableRandomQuestionOrder)
                    {
                        var random = new Random();
                        // 先随机打乱，再按题型排序，确保同类型题目相对顺序一致
                        questionsList = questionsList.OrderBy(q => random.Next()).OrderBy(q => q.TypeValue).ToList();
                    }
                    else
                    {
                        questionsList = questionsList.OrderBy(q => q.TypeValue).ToList();
                    }

                    // 创建答题记录
                    var answerRecords = new List<ExamAnswerRecord>();
                    for (int i = 0; i < questionsList.Count; i++)
                    {
                        var question = questionsList[i];
                        answerRecords.Add(new ExamAnswerRecord
                        {
                            ExamRecordId = examRecord.Id,
                            QuestionId = question.QuestionId,
                            QuestionVersionId = question.QuestionVersionId,
                            OrderNumber = i + 1,
                            IsMarked = false
                        });
                    }

                    await _answerRecordRepository.AddRangeAsync(answerRecords);

                    // ✅ 验证答题记录是否成功创建
                    if (answerRecords.Count == 0)
                    {
                        throw new BusinessException("答题记录创建失败");
                    }

                    _logger.LogInformation("✅ 成功创建考试记录和答题记录（事务已提交），考试ID: {ExamId}, 记录ID: {RecordId}, 学生ID: {StudentId}, 题目数: {QuestionCount}",
                        examId, examRecord.Id, studentId, answerRecords.Count);
                });

                // 清除相关缓存，确保数据一致性（在事务外执行，失败不影响主流程）
                try
                {
                    if (existingRecord != null && existingRecord.CreatedBy != 0)
                    {
                        // 清除用户考试记录缓存
                        await _examCacheService.ClearUserExamRecordCacheAsync(examId, existingRecord.CreatedBy);
                    }
                }
                catch (Exception ex)
                {
                    // 缓存清理失败不应影响主流程，只记录日志
                    _logger.LogWarning(ex, "清除考试记录缓存时发生错误: ExamId={ExamId}, StudentId={StudentId}", examId, studentId);
                }

                return examRecord!;
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "获取考试创建锁超时: {LockKey}, 考试ID: {ExamId}, 学生ID: {StudentId}", lockKey, examId, studentId);
            throw new AppServiceException(423, "系统繁忙，请稍后再试");
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException && ex is not AppServiceException)
        {
            _logger.LogError(ex, "创建考试记录过程发生错误: 考试ID={ExamId}, 学生ID={StudentId}", examId, studentId);
            throw;
        }
    }

    /// <summary>
    /// 记录切屏事件
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="userIp">用户IP地址</param>
    /// <returns>任务完成状态</returns>
    public async Task RecordScreenSwitchForClientAsync(long recordId, long studentId, string userIp)
    {
        try
        {
            // 获取考试记录
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .Where(r => r.Id == recordId && r.StudentId == studentId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new ArgumentException("考试记录不存在", nameof(recordId));
            }

            // 检查考试状态
            if (examRecord.Status != ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试已结束，无法记录切屏");
            }

            // 更新IP地址（如果提供了新的IP且不同于原IP）
            if (!string.IsNullOrEmpty(userIp) && examRecord.IpAddress != userIp)
            {
                examRecord.IpAddress = userIp;

                // 如果IP变更，可能是作弊行为，记录
                var cheatingSuspicionRecord = string.IsNullOrEmpty(examRecord.CheatingSuspicionRecord)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(examRecord.CheatingSuspicionRecord);

                if (cheatingSuspicionRecord == null)
                {
                    cheatingSuspicionRecord = new List<string>();
                }

                //这里记录当前时间及IP变更信息
                cheatingSuspicionRecord.Add($"IP变更（{DateTime.Now:yyyy-MM-dd HH:mm:ss}）：从 {examRecord.IpAddress} 变更为 {userIp}");

                examRecord.CheatingSuspicionRecord = System.Text.Json.JsonSerializer.Serialize(cheatingSuspicionRecord);

                // 增加作弊嫌疑等级
                examRecord.CheatingSuspicionLevel = Math.Min(100, examRecord.CheatingSuspicionLevel + 20);
            }

            // 增加切屏次数
            examRecord.ScreenSwitchCount += 1;

            // 更新作弊嫌疑等级
            int maxAllowedSwitches = examRecord.ExamSetting.AllowedScreenSwitchCount;
            if (maxAllowedSwitches > 0 && examRecord.ScreenSwitchCount > maxAllowedSwitches)
            {
                // 超过允许的切屏次数，提高作弊嫌疑等级
                int exceedCount = examRecord.ScreenSwitchCount - maxAllowedSwitches;
                int suspicionIncrease = 10 * exceedCount; // 每超过一次增加10点嫌疑

                examRecord.CheatingSuspicionLevel += suspicionIncrease;
                if (examRecord.CheatingSuspicionLevel > 100)
                {
                    examRecord.CheatingSuspicionLevel = 100; // 最大不超过100
                }

                // 记录作弊嫌疑记录
                var cheatingSuspicionRecord = string.IsNullOrEmpty(examRecord.CheatingSuspicionRecord)
                    ? new List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<string>>(examRecord.CheatingSuspicionRecord);

                if (cheatingSuspicionRecord == null)
                {
                    cheatingSuspicionRecord = new List<string>();
                }

                //这里记录当前时间及切屏超限信息
                cheatingSuspicionRecord.Add($"切屏超限（{DateTime.Now:yyyy-MM-dd HH:mm:ss}）：累计切屏 {examRecord.ScreenSwitchCount} 次，超过限制 {exceedCount} 次");

                examRecord.CheatingSuspicionRecord = System.Text.Json.JsonSerializer.Serialize(cheatingSuspicionRecord);
            }

            // 保存更改
            await Repository.UpdateAsync(examRecord);
            
                // ⚡⚡⚡ 极致性能优化：更新考试记录缓存（Write-Through模式），避免缓存穿透
                try
                {
                    var cacheKey = new ExamCacheOptions.ExamRecordCache(recordId);
                    var updatedCache = new ExamRecordCacheDto
                    {
                        Id = examRecord.Id,
                        ExamSettingId = examRecord.ExamSettingId,
                        StudentId = examRecord.StudentId,
                        Status = examRecord.Status,
                        StartTime = examRecord.StartTime,
                        ScreenSwitchCount = examRecord.ScreenSwitchCount // 更新切屏次数
                    };
                    await _cacheService.SetAsync(cacheKey.Key, updatedCache, cacheKey.Options);
                    _logger.LogDebug("切屏记录后已更新考试记录缓存: RecordId={RecordId}, ScreenSwitchCount={Count}", 
                        recordId, examRecord.ScreenSwitchCount);
                }
                catch (Exception ex)
                {
                    // 缓存更新失败不影响主流程，但记录警告
                    _logger.LogWarning(ex, "更新考试记录缓存失败: RecordId={RecordId}", recordId);
                }
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw;
        }
    }

    /// <summary>
    /// 提交考试答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="answers">可选的答案列表，用于最后提交前保存</param>
    /// <returns>提交结果，包含是否成功和是否可查看结果</returns>
    public async Task<(bool Success, bool EnableViewResult)> SubmitExamForClientAsync(long recordId, long studentId, List<ClientExamAnswerDto>? answers = null)
    {
        // 使用分布式锁防止并发提交
        var lockKey = $"exam_submit_{recordId}_{studentId}";

        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(1)))
            {
                _logger.LogInformation("已获取考试提交锁: {LockKey}", lockKey);

                // 获取考试记录
                var examRecord = await Repository.CreateQuery()
                    .Include(r => r.ExamSetting)
                    .ThenInclude(s => s.ExamPaper)
                    .Where(r => r.Id == recordId && r.StudentId == studentId)
                    .FirstOrDefaultAsync();

                if (examRecord == null)
                {
                    throw new AppServiceException(409, "考试记录不存在");
                }

                // 重新查询状态，确保在获取锁期间状态未被更改
                if (examRecord.Status != ExamRecordStatus.InProgress)
                {
                    _logger.LogWarning("考试已提交，不能重复提交: 记录ID={RecordId}, 学生ID={StudentId}, 当前状态={Status}",
                        recordId, studentId, examRecord.Status);
                    throw new AppServiceException(409, "考试已提交，不能重复提交");
                }

                // 加载试卷题目
                var examPaper = await _examSettingRepository.CreateQuery()
                    .Include(es => es.ExamPaper)
                    .ThenInclude(ep => ep.ExamPaperQuestions)
                    .FirstOrDefaultAsync(es => es.Id == examRecord.ExamSettingId);

                // 如果提供了未保存的答案，先保存这些答案
                if (answers != null && answers.Any())
                {
                    _logger.LogInformation("提交前保存最后 {Count} 个答案", answers.Count);
                    await SubmitAnswersAsync(recordId, answers);
                }

                var now = DateTime.UtcNow;
                examRecord.SubmitTime = now;
                examRecord.Status = ExamRecordStatus.Submitted;
                examRecord.Duration = (int)Math.Ceiling((now - examRecord.CreatedAt).TotalMinutes);

                // 更新考试记录
                await Repository.UpdateAsync(examRecord);

                // 如果是客观题，可以自动评分
                await AutoGradeObjectiveQuestions(examRecord);

                // ✅ 设置已完成考试次数缓存（3小时过期）
                var completedCountCacheKey = new ExamCacheOptions.CompletedExamCount(examRecord.ExamSettingId, studentId);
                int? currentCountValue = await _cacheService.GetAsync<int>(completedCountCacheKey.Key);
                var currentCount = currentCountValue ?? 0;
                var newCount = currentCount + 1;
                await _cacheService.SetAsync(completedCountCacheKey.Key, newCount, completedCountCacheKey.Options);
                
                // ⚡⚡⚡ 极致性能优化：更新考试记录缓存（Write-Through模式），避免缓存穿透
                try
                {
                    var cacheKey = new ExamCacheOptions.ExamRecordCache(recordId);
                    var updatedCache = new ExamRecordCacheDto
                    {
                        Id = examRecord.Id,
                        ExamSettingId = examRecord.ExamSettingId,
                        StudentId = examRecord.StudentId,
                        Status = ExamRecordStatus.Submitted, // 已提交
                        StartTime = examRecord.StartTime,
                        ScreenSwitchCount = examRecord.ScreenSwitchCount
                    };
                    await _cacheService.SetAsync(cacheKey.Key, updatedCache, cacheKey.Options);
                    _logger.LogDebug("已更新考试记录缓存: RecordId={RecordId}, Status=Submitted", recordId);
                }
                catch (Exception ex)
                {
                    // 缓存更新失败不影响主流程，但记录警告
                    _logger.LogWarning(ex, "更新考试记录缓存失败: RecordId={RecordId}", recordId);
                }
                
                _logger.LogInformation("考试提交成功: 记录ID={RecordId}, 学生ID={StudentId}, 已完成次数={CompletedCount}",
                    recordId, studentId, newCount);

                // 返回提交成功状态和是否可以查看结果的设置
                return (true, examRecord.ExamSetting.EnableViewResult);
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "获取考试提交锁超时: {LockKey}", lockKey);
            throw new AppServiceException(423, "系统繁忙，请稍后再试");
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException && ex is not AppServiceException)
        {
            _logger.LogError(ex, "考试提交过程发生错误: 记录ID={RecordId}, 学生ID={StudentId}", recordId, studentId);
            throw;
        }
    }

    /// <summary>
    /// 获取学生的考试历史记录
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <returns>历史考试记录</returns>
    public async Task<List<ClientExamHistoryDto>> GetExamHistoryForClientAsync(long studentId)
    {
        var examHistory = await Repository.CreateQuery()
                .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .Where(r => r.StudentId == studentId)
                .Where(r => r.Status == ExamRecordStatus.Graded || r.Status == ExamRecordStatus.Submitted)
                .OrderByDescending(r => r.StartTime)
                .Select(r => new ClientExamHistoryDto
                {
                    Id = r.Id,
                    ExamId = r.ExamSettingId,
                    Name = r.ExamSetting.Name,
                    StartTime = r.StartTime,
                    SubmitTime = r.SubmitTime,
                    Duration = r.Duration ?? r.ExamSetting.Duration,
                    Score = r.Score,
                    TotalScore = r.ExamSetting.ExamPaper.TotalScore,
                    IsPassed = r.IsPassed,
                    Status = r.Status.ToString()
                })
                .ToListAsync();

        return examHistory;
    }

    /// <summary>
    /// 获取考试结果（客户端视图）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试结果</returns>
    public async Task<ClientExamResultDto> GetExamResultForClientAsync(long recordId, long studentId)
    {
        try
        {
            var examRecord = await Repository.CreateQuery()
                .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .ThenInclude(pq => pq.Question)
                .Include(r => r.ExamSetting)
                .ThenInclude(s => s.ExamPaper)
                .ThenInclude(p => p.ExamPaperQuestions)
                .ThenInclude(pq => pq.QuestionVersion)
                .Include(r => r.AnswerRecords)
                .Where(r => r.Id == recordId && r.StudentId == studentId)
                .FirstOrDefaultAsync();

            if (examRecord == null)
            {
                throw new ArgumentException("考试记录不存在", nameof(recordId));
            }

            if (examRecord.Status == ExamRecordStatus.InProgress)
            {
                throw new InvalidOperationException("考试尚未提交，无法查看结果");
            }

            // 加载答案记录的题目关系
            var answerRecords = await _answerRecordRepository.CreateQuery()
                .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
                .Include(a => a.Question)
                .Include(a => a.QuestionVersion)
                .Where(a => a.ExamRecordId == recordId)
                .ToListAsync();

            var examPaper = examRecord.ExamSetting.ExamPaper;

            // 构建试卷信息（包含换算配置）
            var examPaperInfo = new ClientExamPaperInfoDto
            {
                Id = examPaper.Id,
                Title = examPaper.Name,
                TotalScore = examPaper.TotalScore,
                PassScore = examPaper.PassScore,
                EnableScoreConversion = examPaper.EnableScoreConversion,
                OriginalTotalScore = examPaper.OriginalTotalScore,
                OriginalPassScore = examPaper.OriginalPassScore,
                ConversionTargetFullScore = examPaper.EnableScoreConversion ? examPaper.TotalScore : null,
                ConversionTargetPassScore = examPaper.EnableScoreConversion ? examPaper.PassScore : null,
                ConversionDecimalPlaces = examPaper.ConversionDecimalPlaces,
                ConversionRatio = examPaper.ConversionRatio,
                ConversionDescription = string.Empty, // 稍后填充
                QuestionCount = examPaper.ExamPaperQuestions?.Count,
                TotalQuestions = examPaper.ExamPaperQuestions?.Count
            };

            // 如果启用了成绩换算，生成换算描述
            if (examPaper.EnableScoreConversion && examPaper.OriginalTotalScore.HasValue)
            {
                examPaperInfo.ConversionDescription = $"将{examPaper.OriginalTotalScore.Value}分制转换为{examPaper.TotalScore}分制";
                if (examPaper.ConversionRatio.HasValue)
                {
                    examPaperInfo.ConversionDescription += $"，换算比例为{examPaper.ConversionRatio.Value:F4}";
                }
            }

            // 获取试卷中的所有题目（包括未作答的）
            var allQuestions = (examPaper.ExamPaperQuestions ?? new List<ExamPaperQuestion>())
                .Where(q => q.Question != null && q.QuestionVersion != null)
                .OrderBy(q => q.Question.Type)
                .ThenBy(q => q.OrderNumber)
                .ToList();

            // 创建答案字典以便快速查找
            var answerDict = answerRecords.ToDictionary(a => a.QuestionId, a => a);

            // 构建所有题目的答案结果（包括未作答的）
            var allAnswerResults = allQuestions.Select(paperQuestion =>
            {
                // 检查是否有对应的答案记录
                var hasAnswer = answerDict.TryGetValue(paperQuestion.QuestionId, out var answerRecord);

                if (hasAnswer && answerRecord != null && answerRecord.QuestionVersion != null && answerRecord.Question != null)
                {
                    // 已作答的题目
                    return new ClientExamAnswerResultDto
                    {
                        QuestionId = answerRecord.QuestionId,
                        Content = answerRecord.QuestionVersion.Content ?? string.Empty,
                        Type = answerRecord.Question.Type.ToString(),
                        Score = Convert.ToInt32(answerRecord.QuestionVersion.DefaultScore),
                        UserAnswer = answerRecord.Question.Type == QuestionType.TrueFalse ?
                            ConvertTrueFalseAnswer(answerRecord.Answer ?? string.Empty) :
                            answerRecord.Answer ?? string.Empty,
                        CorrectAnswer = answerRecord.Question.Type == QuestionType.TrueFalse ?
                            ConvertTrueFalseAnswer(answerRecord.QuestionVersion.CorrectAnswer ?? string.Empty) :
                            answerRecord.QuestionVersion.CorrectAnswer ?? string.Empty,
                        IsCorrect = answerRecord.IsCorrect ?? false,
                        ObtainedScore = answerRecord.Score ?? 0,
                        IsAnswered = true
                    };
                }
                else
                {
                    // 未作答的题目
                    return new ClientExamAnswerResultDto
                    {
                        QuestionId = paperQuestion.QuestionId,
                        Content = paperQuestion.QuestionVersion?.Content ?? string.Empty,
                        Type = paperQuestion.Question?.Type.ToString() ?? string.Empty,
                        Score = Convert.ToInt32(paperQuestion.QuestionVersion?.DefaultScore ?? 0),
                        UserAnswer = string.Empty,
                        CorrectAnswer = paperQuestion.Question?.Type == QuestionType.TrueFalse ?
                            ConvertTrueFalseAnswer(paperQuestion.QuestionVersion?.CorrectAnswer ?? string.Empty) :
                            paperQuestion.QuestionVersion?.CorrectAnswer ?? string.Empty,
                        IsCorrect = false,
                        ObtainedScore = 0,
                        IsAnswered = false
                    };
                }
            }).ToList();

            var result = new ClientExamResultDto
            {
                Id = examRecord.Id,
                ExamId = examRecord.ExamSettingId,
                Name = examRecord.ExamSetting.Name,
                StartTime = examRecord.StartTime,
                SubmitTime = examRecord.SubmitTime,
                Duration = examRecord.Duration ?? 0,
                Score = examRecord.Score,
                TotalScore = examRecord.ExamSetting.ExamPaper.TotalScore,
                IsPassed = examRecord.IsPassed,
                Status = examRecord.Status.ToString(),
                Comments = examRecord.Comments,
                // 添加成绩换算相关字段
                OriginalScore = examRecord.OriginalScore,
                IsScoreConverted = examRecord.IsScoreConverted,
                ScoreConversionRatio = examRecord.ScoreConversionRatio,
                Exam = examPaperInfo,
                Answers = allAnswerResults,
                EnableQuestionAnalysis = examRecord.ExamSetting.EnableQuestionAnalysis
            };

            return result;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            throw;
        }
    }

    /// <summary>
    /// 将判断题的True/False答案转换为"对"/"错"
    /// </summary>
    /// <param name="answer">原始答案</param>
    /// <returns>转换后的答案</returns>
    private string ConvertTrueFalseAnswer(string? answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            return string.Empty;
        }

        return answer.Equals("True", StringComparison.OrdinalIgnoreCase) ? "✓" : "✗";
    }

    // 客观题自动评分
    private async Task AutoGradeObjectiveQuestions(ExamRecord examRecord)
    {
        // 加载所有答案记录
        var answerRecords = await _answerRecordRepository.CreateQuery()
            .Where(a => a.ExamRecordId == examRecord.Id)
            .ToListAsync();

        // 加载所有答案关联的题目和题目版本
        foreach (var answer in answerRecords)
        {
            answer.Question = await _questionVersionRepository.CreateQuery()
                .Include(qv => qv.Question)
                .Where(qv => qv.Id == answer.QuestionVersionId)
                .Select(qv => qv.Question)
                .FirstOrDefaultAsync();

            answer.QuestionVersion = await _questionVersionRepository.GetByIdAsync(answer.QuestionVersionId);
        }

        // 使用评分器进行评分
        var grader = new Graders.ObjectiveQuestionGrader();
        var result = grader.Grade(answerRecords, examRecord.ExamSetting.ExamPaper.PassScore);

        // 如果全部为客观题，更新考试记录状态
        if (result.IsAllObjective)
        {
            await ApplyScoreConversion(examRecord, result.TotalScore);
            examRecord.Status = ExamRecordStatus.Graded;
            examRecord.GradedTime = DateTime.UtcNow;

            await Repository.UpdateAsync(examRecord);
        }
    }

    /// <summary>
    /// 获取考试的所有已保存答案
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>答案列表</returns>
    public async Task<List<ExamAnswerRecord>> GetExamAnswersAsync(long recordId)
    {
        return await _answerRecordRepository.CreateQuery()
            .AsNoTracking() // 🚀 性能优化：只读查询无需跟踪
            .Where(a => a.ExamRecordId == recordId)
            .OrderBy(a => a.OrderNumber)
            .ToListAsync();
    }

    /// <summary>
    /// 重新批改
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="modifyExamScoreDto">重新批改参数</param>
    /// <returns>修改后的考试记录</returns>
    public async Task<ExamRecordDto> ModifyExamScoreAsync(long recordId, ModifyExamScoreDto modifyExamScoreDto)
    {
        // 获取考试记录及相关信息
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.ExamSetting)
            .ThenInclude(es => es.ExamPaper)
            .Include(r => r.AnswerRecords)
            .ThenInclude(a => a.QuestionVersion)
            .ThenInclude(qv => qv.Question)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        // 验证考试状态
        if (examRecord.Status == ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试尚未完成，无法修改分数");
        }

        // 获取试卷信息
        var examPaper = examRecord.ExamSetting?.ExamPaper;
        if (examPaper == null)
        {
            throw new BusinessException("试卷信息不存在");
        }

        // 验证目标分数不超过试卷总分
        if (modifyExamScoreDto.TargetScore > examPaper.TotalScore)
        {
            throw new BusinessException($"目标分数不能超过试卷总分 {examPaper.TotalScore}");
        }

        if (modifyExamScoreDto.TargetScore < 0)
        {
            throw new BusinessException("目标分数不能小于0");
        }

        // 区分客观题和非客观题
        var objectiveQuestionTypes = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse };
        var answerRecords = examRecord.AnswerRecords.ToList();

        var objectiveAnswers = answerRecords
            .Where(a => a.QuestionVersion?.Question != null && objectiveQuestionTypes.Contains(a.QuestionVersion.Question.Type))
            .ToList();

        var subjectiveAnswers = answerRecords
            .Where(a => a.QuestionVersion?.Question != null && !objectiveQuestionTypes.Contains(a.QuestionVersion.Question.Type))
            .ToList();

        // 计算当前客观题总分和非客观题总分
        double currentObjectiveScore = objectiveAnswers.Sum(a => a.Score ?? 0);
        double currentSubjectiveScore = subjectiveAnswers.Sum(a => a.Score ?? 0);
        double currentTotalScore = currentObjectiveScore + currentSubjectiveScore;

        // 计算需要调整的目标分数与当前分数的差异
        double targetTotalScore = modifyExamScoreDto.TargetScore;
        double scoreDifference = targetTotalScore - currentTotalScore;

        // 如果分数差异很小，无需调整
        if (Math.Abs(scoreDifference) < 0.01)
        {
            // 只更新状态和批改时间
            // 应用成绩换算
            await ApplyScoreConversion(examRecord, targetTotalScore);

            examRecord.Status = ExamRecordStatus.Graded;
            examRecord.GradedTime = examRecord.SubmitTime; // 确保批改时间与提交时间一致
            examRecord.UpdatedAt = examRecord.SubmitTime; // 确保更新时间与提交时间一致

            // 更新答案记录的时间字段
            foreach (var answer in answerRecords)
            {
                // 生成一个开始时间和提交时间之间的随机时间
                var randomTime = GenerateRandomTimeBetween(examRecord.StartTime, examRecord.SubmitTime);
                answer.GradedTime = randomTime; // 答案批改时间使用随机时间
                answer.UpdatedAt = randomTime; // 答案更新时间使用随机时间
            }

            // 保存更改
            await _answerRecordRepository.UpdateRangeAsync(answerRecords);
            await Repository.UpdateAsync(examRecord);
            return Mapper.Map<ExamRecordDto>(examRecord);
        }

        // 如果需要增加分数，先尝试使用自动评分重新评估客观题
        if (scoreDifference > 0 && objectiveAnswers.Any())
        {
            // 重置客观题评分状态
            foreach (var answer in objectiveAnswers)
            {
                answer.IsCorrect = null;
                answer.Score = null;
            }

            // 使用自动评分方法重新评分
            await AutoGradeObjectiveQuestions(examRecord);

            // 重新计算调整后的分数差异
            double newObjectiveScore = objectiveAnswers.Sum(a => a.Score ?? 0);
            double newTotalScore = newObjectiveScore + currentSubjectiveScore;
            scoreDifference = targetTotalScore - newTotalScore;
        }

        // 如果仍然需要调整分数
        if (Math.Abs(scoreDifference) > 0.01)
        {
            if (scoreDifference > 0)
            {
                // 需要增加分数
                AdjustScoreUpward(objectiveAnswers, scoreDifference);

                // 如果客观题已经全部得满分，但仍需增加分数
                double remainingScoreToAdd = targetTotalScore - (objectiveAnswers.Sum(a => a.Score ?? 0) + currentSubjectiveScore);

                // 如果还有剩余分数需要添加且有非客观题
                if (remainingScoreToAdd > 0.01 && subjectiveAnswers.Any())
                {
                    AdjustScoreUpward(subjectiveAnswers, remainingScoreToAdd);
                }
            }
            else
            {
                // 需要减少分数
                double scoreToReduce = Math.Abs(scoreDifference);

                // 优先从客观题中减分
                if (currentObjectiveScore >= scoreToReduce)
                {
                    AdjustScoreDownward(objectiveAnswers, scoreToReduce);
                }
                else
                {
                    // 客观题分数不足，先减完客观题
                    if (currentObjectiveScore > 0)
                    {
                        AdjustScoreDownward(objectiveAnswers, currentObjectiveScore);
                    }

                    // 剩余从主观题中减分
                    double remainingScoreToReduce = scoreToReduce - currentObjectiveScore;
                    if (remainingScoreToReduce > 0.01 && subjectiveAnswers.Any())
                    {
                        AdjustScoreDownward(subjectiveAnswers, remainingScoreToReduce);
                    }
                }
            }
        }

        // 最终检查和调整，确保总分符合目标
        double adjustedTotalScore = answerRecords.Sum(a => a.Score ?? 0);

        // 如果调整后分数与目标分数有较大差异，进行最后的微调
        if (Math.Abs(adjustedTotalScore - targetTotalScore) > 0.1)
        {
            // 找出得分最高的题目进行调整
            var adjustableAnswer = answerRecords
                .Where(a => a.Score > 0)
                .OrderByDescending(a => a.Score)
                .FirstOrDefault();

            if (adjustableAnswer != null)
            {
                adjustableAnswer.Score = adjustableAnswer.Score + (targetTotalScore - adjustedTotalScore);

                // 确保分数不为负数
                if (adjustableAnswer.Score < 0)
                {
                    adjustableAnswer.Score = 0;
                }
            }
        }

        // 更新考试记录
        examRecord.Score = answerRecords.Sum(a => a.Score ?? 0);
        examRecord.IsPassed = examRecord.Score >= examPaper.PassScore;
        examRecord.Status = ExamRecordStatus.Graded;
        examRecord.GradedTime = examRecord.SubmitTime; // 确保批改时间与提交时间一致
        examRecord.UpdatedAt = examRecord.SubmitTime; // 确保更新时间与提交时间一致
        examRecord.Comments += "-";

        // 更新答案记录的时间字段
        foreach (var answer in answerRecords)
        {
            var randomTime = GenerateRandomTimeBetween(examRecord.StartTime, examRecord.SubmitTime);
            answer.GradedTime = randomTime;
            answer.UpdatedAt = randomTime;
            answer.CreatedAt = randomTime;
        }

        // 保存更改
        await _answerRecordRepository.UpdateRangeAsync(answerRecords);
        await Repository.UpdateAsync(examRecord);

        // 返回更新后的考试记录
        return Mapper.Map<ExamRecordDto>(examRecord);
    }


    /// <summary>
    /// 提高分数
    /// </summary>
    private void AdjustScoreUpward(List<ExamAnswerRecord> answerRecords, double scoreToAdd)
    {
        // 按照未得满分的题目进行排序（优先调整分值较大的题目）
        var adjustableAnswers = answerRecords
            .Where(a => (a.Score ?? 0) < a.QuestionVersion.DefaultScore)
            .OrderByDescending(a => a.QuestionVersion.DefaultScore - (a.Score ?? 0))
            .ToList();

        double remainingScoreToAdd = scoreToAdd;

        // 首先处理未得满分的题目
        foreach (var answer in adjustableAnswers)
        {
            if (remainingScoreToAdd <= 0) break;

            double currentScore = answer.Score ?? 0;
            double maxScore = answer.QuestionVersion.DefaultScore;
            double scoreGap = maxScore - currentScore;

            if (scoreGap <= 0) continue;

            // 计算可以添加的分数
            double scoreToAddForThisAnswer = Math.Min(scoreGap, remainingScoreToAdd);

            // 更新答案记录
            answer.Score = currentScore + scoreToAddForThisAnswer;
            answer.IsCorrect = Math.Abs(answer.Score.Value - maxScore) < 0.01; // 如果得满分则标记为正确

            // 如果答案变为正确，需要更新答案内容与标准答案一致
            if (answer.IsCorrect == true)
            {
                answer.Answer = answer.QuestionVersion.CorrectAnswer;
            }
            // 如果是多选题且只是部分加分，根据分数比例生成合理的答案
            else if (answer.QuestionVersion.Question.Type == QuestionType.MultipleChoice)
            {
                try
                {
                    var correctOptionsArray = answer.QuestionVersion.CorrectAnswer.Split(',').Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
                    if (correctOptionsArray.Length > 1)
                    {
                        // 按照得分比例选择部分正确选项
                        int optionsToSelect = (int)Math.Ceiling((answer.Score.Value / maxScore) * correctOptionsArray.Length);
                        optionsToSelect = Math.Min(optionsToSelect, correctOptionsArray.Length); // 确保不超过选项数量
                        optionsToSelect = Math.Max(1, optionsToSelect); // 确保至少选择一个选项

                        answer.Answer = string.Join(",", correctOptionsArray.Take(optionsToSelect));
                    }
                }
                catch (Exception)
                {
                    // 如果解析失败，保持原答案不变
                }
            }

            remainingScoreToAdd -= scoreToAddForThisAnswer;
        }

        // 如果还有剩余分数未加完，从零分题目开始加分
        if (remainingScoreToAdd > 0)
        {
            var zeroScoreAnswers = answerRecords
                .Where(a => (a.Score ?? 0) == 0)
                .OrderByDescending(a => a.QuestionVersion.DefaultScore)
                .ToList();

            foreach (var answer in zeroScoreAnswers)
            {
                if (remainingScoreToAdd <= 0) break;

                double maxScore = answer.QuestionVersion.DefaultScore;

                // 计算可以添加的分数
                double scoreToAddForThisAnswer = Math.Min(maxScore, remainingScoreToAdd);

                // 更新答案记录
                answer.Score = scoreToAddForThisAnswer;
                answer.IsCorrect = Math.Abs(answer.Score.Value - maxScore) < 0.01;

                // 如果答案变为正确，需要更新答案内容与标准答案一致
                if (answer.IsCorrect == true)
                {
                    answer.Answer = answer.QuestionVersion.CorrectAnswer;
                }
                // 如果是部分得分，可以根据题型决定如何修改答案
                else if (answer.QuestionVersion.Question.Type == QuestionType.MultipleChoice)
                {
                    try
                    {
                        var correctOptionsArray = answer.QuestionVersion.CorrectAnswer.Split(',').Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
                        if (correctOptionsArray.Length > 1)
                        {
                            // 按照得分比例选择部分正确选项
                            int optionsToSelect = (int)Math.Ceiling((scoreToAddForThisAnswer / maxScore) * correctOptionsArray.Length);
                            optionsToSelect = Math.Min(optionsToSelect, correctOptionsArray.Length); // 确保不超过选项数量
                            optionsToSelect = Math.Max(1, optionsToSelect); // 确保至少选择一个选项

                            answer.Answer = string.Join(",", correctOptionsArray.Take(optionsToSelect));
                        }
                        else if (correctOptionsArray.Length == 1)
                        {
                            // 如果只有一个正确选项，且得分超过一半，则设为正确答案
                            answer.Answer = scoreToAddForThisAnswer >= (maxScore / 2) ?
                                answer.QuestionVersion.CorrectAnswer :
                                GenerateIncorrectAnswer(answer.QuestionVersion);
                        }
                    }
                    catch (Exception)
                    {
                        // 如果解析失败，生成一个错误答案
                        answer.Answer = GenerateIncorrectAnswer(answer.QuestionVersion);
                    }
                }
                else
                {
                    // 其他题型如果是部分得分，生成一个接近正确但不完全正确的答案
                    answer.Answer = GeneratePartiallyCorrectAnswer(answer.QuestionVersion, scoreToAddForThisAnswer / maxScore);
                }

                remainingScoreToAdd -= scoreToAddForThisAnswer;
            }
        }

        // 特殊情况：如果所有题目都已得满分，但仍需增加分数
        if (remainingScoreToAdd > 0.01)
        {
            // 找出分值最大的题目，分配额外分数
            var highestScoreAnswer = answerRecords
                .OrderByDescending(a => a.QuestionVersion.DefaultScore)
                .FirstOrDefault();

            if (highestScoreAnswer != null)
            {
                highestScoreAnswer.Score = (highestScoreAnswer.Score ?? 0) + remainingScoreToAdd;
                highestScoreAnswer.IsCorrect = true;
                highestScoreAnswer.Answer = highestScoreAnswer.QuestionVersion.CorrectAnswer;
            }
        }
    }

    /// <summary>
    /// 降低分数
    /// </summary>
    private void AdjustScoreDownward(List<ExamAnswerRecord> answerRecords, double scoreToReduce)
    {
        // 按照已得分的题目进行排序（优先从分值较小的题目开始扣分）
        var adjustableAnswers = answerRecords
            .Where(a => (a.Score ?? 0) > 0)
            .OrderBy(a => a.Score)
            .ToList();

        double remainingScoreToReduce = scoreToReduce;

        foreach (var answer in adjustableAnswers)
        {
            if (remainingScoreToReduce <= 0) break;

            double currentScore = answer.Score ?? 0;
            if (currentScore <= 0) continue;

            double maxScore = answer.QuestionVersion.DefaultScore;

            // 计算可以减少的分数
            double scoreToReduceForThisAnswer = Math.Min(currentScore, remainingScoreToReduce);

            // 更新答案记录
            answer.Score = currentScore - scoreToReduceForThisAnswer;

            // 只有在分数为0或接近0时才标记为不正确
            answer.IsCorrect = answer.Score > 0.01;

            // 如果分数变为0或接近0，更新答案内容为错误答案
            if (answer.Score <= 0.01)
            {
                // 设置分数为0以避免舍入误差
                answer.Score = 0;
                answer.IsCorrect = false;

                // 根据题型生成一个错误答案
                answer.Answer = GenerateIncorrectAnswer(answer.QuestionVersion);
            }
            // 如果是部分扣分，可以根据剩余分数比例修改答案
            else if (answer.QuestionVersion.Question.Type == QuestionType.MultipleChoice)
            {
                try
                {
                    var correctOptionsArray = answer.QuestionVersion.CorrectAnswer.Split(',').Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();
                    if (correctOptionsArray.Length > 1)
                    {
                        // 按照剩余得分比例选择部分正确选项
                        double scoreRatio = answer.Score.Value / maxScore;
                        int optionsToSelect = (int)Math.Ceiling(scoreRatio * correctOptionsArray.Length);
                        optionsToSelect = Math.Min(optionsToSelect, correctOptionsArray.Length); // 确保不超过选项数量
                        optionsToSelect = Math.Max(1, optionsToSelect); // 确保至少选择一个选项（除非分数为0）

                        if (optionsToSelect > 0)
                        {
                            answer.Answer = string.Join(",", correctOptionsArray.Take(optionsToSelect));
                        }
                        else
                        {
                            answer.Answer = GenerateIncorrectAnswer(answer.QuestionVersion);
                        }
                    }
                    else if (correctOptionsArray.Length == 1)
                    {
                        // 如果只有一个正确选项，且得分小于一半，则设为错误答案
                        answer.Answer = answer.Score >= (maxScore / 2) ?
                            answer.QuestionVersion.CorrectAnswer :
                            GenerateIncorrectAnswer(answer.QuestionVersion);
                    }
                }
                catch (Exception)
                {
                    // 如果解析失败，生成一个错误答案
                    answer.Answer = GenerateIncorrectAnswer(answer.QuestionVersion);
                }
            }
            else
            {
                // 其他题型如果还有部分得分，生成一个接近正确但不完全正确的答案
                answer.Answer = GeneratePartiallyCorrectAnswer(answer.QuestionVersion, answer.Score.Value / maxScore);
            }

            remainingScoreToReduce -= scoreToReduceForThisAnswer;
        }
    }

    /// <summary>
    /// 生成错误答案
    /// </summary>
    private string GenerateIncorrectAnswer(QuestionVersion questionVersion)
    {
        if (questionVersion == null || questionVersion.Question == null)
            return "";

        switch (questionVersion.Question.Type)
        {
            case QuestionType.SingleChoice:
                // 使用题目的实际选项而不是ABCD
                var correctAnswer = questionVersion.CorrectAnswer;
                if (questionVersion.Options != null && questionVersion.Options.Any())
                {
                    // 从选项中选择一个与正确答案不同的选项索引
                    var availableOptions = Enumerable.Range(0, questionVersion.Options.Count)
                        .Select(i => i.ToString())
                        .Where(i => i != correctAnswer)
                        .ToList();

                    if (availableOptions.Any())
                    {
                        // 随机选择一个错误选项
                        var random = new Random();
                        int randomIndex = random.Next(availableOptions.Count);
                        return availableOptions[randomIndex];
                    }
                }
                // 如果无法获取选项，返回一个默认的错误答案
                return correctAnswer == "0" ? "1" : "0";

            case QuestionType.MultipleChoice:
                // 生成一个有意义的错误多选答案
                if (questionVersion.Options != null && questionVersion.Options.Any() &&
                    !string.IsNullOrEmpty(questionVersion.CorrectAnswer))
                {
                    try
                    {
                        var correctOptions = questionVersion.CorrectAnswer.Split(',')
                            .Where(o => !string.IsNullOrWhiteSpace(o))
                            .Select(o => o.Trim())
                            .ToHashSet();

                        // 所有可能的选项索引
                        var allOptionIndices = Enumerable.Range(0, questionVersion.Options.Count)
                            .Select(i => i.ToString())
                            .ToList();

                        // 找出错误选项
                        var incorrectOptions = allOptionIndices
                            .Where(i => !correctOptions.Contains(i))
                            .ToList();

                        // 如果有错误选项，返回其中一个或组合
                        if (incorrectOptions.Any())
                        {
                            // 随机选择1-2个错误选项
                            var random = new Random();
                            int count = Math.Min(incorrectOptions.Count, random.Next(1, 3));

                            return string.Join(",", incorrectOptions.OrderBy(x => random.Next()).Take(count));
                        }

                        // 如果没有错误选项（所有选项都是正确的），则返回部分正确选项
                        if (correctOptions.Count > 1)
                        {
                            return correctOptions.First();
                        }
                    }
                    catch
                    {
                        // 发生异常，返回空
                        return "";
                    }
                }
                return "";

            case QuestionType.TrueFalse:
                // 返回与正确答案相反的选项
                return questionVersion.CorrectAnswer.Equals("True", StringComparison.OrdinalIgnoreCase) ?
                    "False" : "True";

            default:
                return "";
        }
    }

    /// <summary>
    /// 生成部分正确的答案（适用于部分得分情况）
    /// </summary>
    private string GeneratePartiallyCorrectAnswer(QuestionVersion questionVersion, double scoreRatio)
    {
        if (questionVersion == null || questionVersion.Question == null)
            return "";

        // 如果分数比例很高（接近满分），返回正确答案
        if (scoreRatio > 0.9)
            return questionVersion.CorrectAnswer;

        // 如果分数比例很低（接近0分），返回错误答案
        if (scoreRatio < 0.1)
            return GenerateIncorrectAnswer(questionVersion);

        // 处理中间情况
        switch (questionVersion.Question.Type)
        {
            case QuestionType.SingleChoice:
                // 单选题只能是对或错，根据分数比例决定
                return scoreRatio >= 0.5 ?
                    questionVersion.CorrectAnswer :
                    GenerateIncorrectAnswer(questionVersion);

            case QuestionType.MultipleChoice:
                try
                {
                    var correctOptions = questionVersion.CorrectAnswer.Split(',')
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToArray();

                    if (correctOptions.Length > 1)
                    {
                        // 根据分数比例选择部分正确选项
                        int optionsToSelect = (int)Math.Ceiling(scoreRatio * correctOptions.Length);
                        optionsToSelect = Math.Min(optionsToSelect, correctOptions.Length);
                        optionsToSelect = Math.Max(1, optionsToSelect);

                        return string.Join(",", correctOptions.Take(optionsToSelect));
                    }
                }
                catch
                {
                    // 发生异常，返回错误答案
                    return GenerateIncorrectAnswer(questionVersion);
                }
                return GenerateIncorrectAnswer(questionVersion);

            case QuestionType.TrueFalse:
                // 判断题只能是对或错，根据分数比例决定
                return scoreRatio >= 0.5 ?
                    questionVersion.CorrectAnswer :
                    (questionVersion.CorrectAnswer == "True" ? "False" : "True");

            default:
                return scoreRatio >= 0.5 ?
                    questionVersion.CorrectAnswer :
                    GenerateIncorrectAnswer(questionVersion);
        }
    }

    /// <summary>
    /// 生成考试开始时间和提交时间之间的随机时间
    /// </summary>
    /// <param name="startTime">考试开始时间</param>
    /// <param name="submitTime">考试提交时间</param>
    /// <returns>随机时间</returns>
    private DateTime GenerateRandomTimeBetween(DateTime startTime, DateTime? submitTime)
    {
        if (!submitTime.HasValue || submitTime <= startTime)
        {
            return startTime;
        }

        var timeSpan = submitTime.Value - startTime;
        var random = new Random();
        var randomSpan = TimeSpan.FromTicks((long)(random.NextDouble() * timeSpan.Ticks));

        return startTime + randomSpan;
    }

    /// <summary>
    /// 获取学生考试试卷详情（用于PDF导出）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试试卷详情</returns>
    public async Task<ExamPaperDetailDto> GetStudentExamPaperDetailAsync(long recordId)
    {
        try
        {
            // 获取考试记录
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .Include(r => r.Student)
                .Include(r => r.AnswerRecords)
                .ThenInclude(a => a.QuestionVersion)
                .ThenInclude(qv => qv.Question)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (examRecord == null)
            {
                throw new BusinessException("考试记录不存在");
            }

            // 获取试卷预览信息
            var preview = await GetAnswerPreviewAsync(recordId);

            // 获取学生信息
            var student = examRecord.Student;
            if (student == null)
            {
                throw new BusinessException("学生信息不存在");
            }

            // 按题型分组
            var questionsByType = examRecord.AnswerRecords
                .GroupBy(a => a.QuestionVersion.Question.Type.ToString())
                .ToDictionary(g => g.Key, g => g.ToList());

            // 计算各题型统计
            var typeStatistics = new List<QuestionTypeStatistics>();

            foreach (var type in questionsByType.Keys)
            {
                var typeAnswers = questionsByType[type];

                var totalScore = typeAnswers.Sum(a => a.QuestionVersion.DefaultScore);
                var obtainedScore = (int)typeAnswers.Sum(a => a.Score ?? 0);
                var correctCount = typeAnswers.Count(a => a.IsCorrect ?? false);

                typeStatistics.Add(new QuestionTypeStatistics
                {
                    Type = type,
                    TypeName = GetQuestionTypeName(type),
                    QuestionCount = typeAnswers.Count,
                    Score = obtainedScore,
                    TotalScore = totalScore,
                    CorrectCount = correctCount
                });
            }

            // 构建试卷题目列表
            var questions = examRecord.AnswerRecords
                .OrderBy(a => a.OrderNumber)
                .Select(a => new ExamPaperQuestionDto
                {
                    QuestionId = a.QuestionId,
                    Type = a.QuestionVersion.Question.Type,
                    Content = a.QuestionVersion.Content,
                    Options = a.QuestionVersion.Options,
                    Score = a.QuestionVersion.DefaultScore,
                    OrderNumber = a.OrderNumber
                })
                .ToList();

            // 构建答案列表
            var answers = examRecord.AnswerRecords
                .OrderBy(a => a.OrderNumber)
                .Select(a => new ClientExamAnswerWithCorrectDto
                {
                    QuestionId = a.QuestionId,
                    Answer = a.Answer,
                    CorrectAnswer = a.QuestionVersion.CorrectAnswer,
                    QuestionType = a.QuestionVersion.Question.Type.ToString(),
                    Score = a.Score,
                    IsCorrect = a.IsCorrect,
                    DefaultScore = a.QuestionVersion.DefaultScore
                })
                .ToList();

            // 构建详情DTO
            var detail = new ExamPaperDetailDto
            {
                ExamPaperId = examRecord.ExamSetting.ExamPaperId,
                ExamRecordId = recordId,
                ExamName = examRecord.ExamSetting.Name,
                StudentId = student.Id,
                StudentName = student.Name,
                IdNo = student.IdNo ?? string.Empty,
                AdmissionTicket = student.AdmissionTicket ?? string.Empty,
                StartTime = examRecord.StartTime,
                SubmitTime = examRecord.SubmitTime,
                Duration = examRecord.Duration,
                TotalScore = (int)(examRecord.Score ?? 0),
                MaxScore = (int)(examRecord.ExamSetting.ExamPaper?.TotalScore ?? 0),
                PassScore = examRecord.ExamSetting.ExamPaper?.PassScore ?? 0,
                IsPassed = examRecord.IsPassed,
                Status = examRecord.Status.ToString(),
                Questions = questions,
                Answers = answers,
                TypeStatistics = typeStatistics
            };

            return detail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生试卷详情失败: {RecordId}", recordId);
            throw new BusinessException($"获取试卷详情失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取题目类型名称
    /// </summary>
    private string GetQuestionTypeName(string type)
    {
        return type switch
        {
            "SingleChoice" => "单选题",
            "MultipleChoice" => "多选题",
            "TrueFalse" => "判断题",
            "ShortAnswer" => "简答题",
            "Essay" => "论述题",
            "Coding" => "编程题",
            _ => type
        };
    }

    /// <summary>
    /// 获取答卷导出设置
    /// </summary>
    /// <returns>答卷导出设置</returns>
    public async Task<ExamPaperExportSettingsDto> GetExamPaperExportSettingsAsync()
    {
        var settings = await _settingsService.GetGlobalSettingAsync<ExamPaperExportSettingsDto>(
            ExamConstants.ExamModule,
            ExamPaperExportSettings);

        return settings ?? new ExamPaperExportSettingsDto();
    }

    /// <summary>
    /// 更新答卷导出设置
    /// </summary>
    /// <param name="settings">答卷导出设置</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateExamPaperExportSettingsAsync(ExamPaperExportSettingsDto settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        // 更新答卷导出设置
        var result = await _settingsService.SetGlobalSettingAsync(
            ExamConstants.ExamModule,
            ExamPaperExportSettings,
            settings);

        return result;
    }

    /// <summary>
    /// 自动批改考试记录
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>批改后的考试记录</returns>
    public async Task<ExamRecordDto> AutoGradeExamRecordAsync(long recordId)
    {
        // 获取考试记录及相关信息
        var examRecord = await Repository.CreateQuery()
            .Include(r => r.ExamSetting)
            .ThenInclude(es => es.ExamPaper)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        if (examRecord == null)
        {
            throw new BusinessException("考试记录不存在");
        }

        // 验证考试状态 - 只能批改已提交的记录
        if (examRecord.Status == ExamRecordStatus.InProgress)
        {
            throw new BusinessException("考试尚未提交，无法进行批改");
        }

        // 获取试卷信息
        var examPaper = examRecord.ExamSetting?.ExamPaper;
        if (examPaper == null)
        {
            throw new BusinessException("试卷信息不存在");
        }

        // 使用现有的自动批改方法
        await AutoGradeObjectiveQuestions(examRecord);

        // 重新加载完整的考试记录数据以返回
        var updatedRecord = await Repository.CreateQuery()
            .Include(r => r.ExamSetting)
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == recordId);

        return Mapper.Map<ExamRecordDto>(updatedRecord);
    }

    /// <summary>
    /// 更新考试记录状态为已提交
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="submitTime">提交时间</param>
    /// <returns>是否更新成功</returns>
    public async Task<bool> UpdateExamRecordToSubmittedAsync(long recordId, DateTime submitTime)
    {
        try
        {
            var examRecord = await Repository.CreateQuery()
                .Include(r => r.ExamSetting)
                .FirstOrDefaultAsync(r => r.Id == recordId);

            if (examRecord == null)
            {
                return false;
            }

            // 只有进行中的考试记录才能更新为已提交
            if (examRecord.Status != ExamRecordStatus.InProgress)
            {
                return false;
            }

            // 更新状态和提交时间
            examRecord.Status = ExamRecordStatus.Submitted;
            examRecord.SubmitTime = submitTime;

            // 计算考试时长
            examRecord.Duration = (int)Math.Ceiling((submitTime - examRecord.StartTime).TotalMinutes);

            // 确保时长不超过考试设置的最大时长
            if (examRecord.ExamSetting != null && examRecord.Duration > examRecord.ExamSetting.Duration)
            {
                examRecord.Duration = examRecord.ExamSetting.Duration;
            }

            await Repository.UpdateAsync(examRecord);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试记录状态为已提交失败，记录ID: {RecordId}", recordId);
            // 记录日志但不抛出异常
            return false;
        }
    }

    /// <summary>
    /// 获取考试记录的答题记录数量
    /// </summary>
    /// <remarks>
    /// ⚠️ 注意：由于CreateExamRecordAsync已使用事务保护，此方法通常不需要调用。
    /// 如果CreateExamRecordAsync成功返回，说明答题记录必然已创建。
    /// 此方法保留用于特殊场景或向后兼容。
    /// </remarks>
    /// <param name="examRecordId">考试记录ID</param>
    /// <returns>答题记录数量</returns>
    public async Task<int> GetAnswerRecordCountAsync(long examRecordId)
    {
        try
        {
            var count = await _answerRecordRepository.CreateQuery()
                .Where(ar => ar.ExamRecordId == examRecordId)
                .CountAsync();

            _logger.LogDebug("获取答题记录数量成功，记录ID: {ExamRecordId}, 数量: {Count}", examRecordId, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取答题记录数量失败，记录ID: {ExamRecordId}", examRecordId);
            throw;
        }
    }
}