using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.Caching.Models;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Dtos.Client;
using CodeSpirit.ExamApi.Dtos.ExamRecord;
using CodeSpirit.ExamApi.Dtos.Student;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.Interfaces;
using Microsoft.Extensions.Logging;
using CodeSpirit.ExamApi.Caching;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.Shared.DistributedLock;

namespace CodeSpirit.ExamApi.Services;

/// <summary>
/// 考试缓存服务
/// </summary>
public class ExamCacheService : IExamCacheService, IScopedDependency
{
    private readonly ICacheService _cacheService;
    private readonly ICacheWarmupService _warmupService;
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly IRepository<ExamAnswerRecord> _answerRecordRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly ILogger<ExamCacheService> _logger;
    private readonly ExamDbContext _context;
    private readonly IDistributedLockProvider _distributedLockProvider;

    // 缓存预热配置
    private static readonly TimeSpan WarmupWindowBeforeStart = TimeSpan.FromMinutes(30); // 考试开始前30分钟开始预热

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="warmupService">缓存预热服务</param>
    /// <param name="examRecordRepository">考试记录仓储</param>
    /// <param name="answerRecordRepository">答题记录仓储</param>
    /// <param name="studentRepository">学生仓储</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="context">数据库上下文</param>
    /// <param name="distributedLockProvider">分布式锁提供程序</param>
    public ExamCacheService(
        ICacheService cacheService,
        ICacheWarmupService warmupService,
        IRepository<ExamRecord> examRecordRepository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<Student> studentRepository,
        ILogger<ExamCacheService> logger,
        ExamDbContext context,
        IDistributedLockProvider distributedLockProvider)
    {
        _cacheService = cacheService;
        _warmupService = warmupService;
        _examRecordRepository = examRecordRepository;
        _answerRecordRepository = answerRecordRepository;
        _studentRepository = studentRepository;
        _logger = logger;
        _context = context;
        _distributedLockProvider = distributedLockProvider;
    }

    /// <summary>
    /// 获取考试基本信息（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本信息</returns>
    public async Task<ExamBasicInfoCacheDto?> GetExamBasicInfoWithCacheAsync(long examId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.BasicInfo(examId),
            async () =>
            {
                _logger.LogDebug("从数据库获取考试基本信息: {ExamId}", examId);

                // 直接从数据库加载考试基本信息，避免依赖其他服务
                var examBasicInfo = await LoadExamBasicInfoFromDatabaseAsync(examId);

                return examBasicInfo;
            });
    }

    /// <summary>
    /// 获取考试题目数据（带缓存，字典格式）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典</returns>
    public async Task<Dictionary<long, ClientExamQuestionDto>?> GetExamQuestionsDataWithCacheAsync(long examId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.Questions(examId),
            async () =>
            {
                _logger.LogDebug("从数据库获取考试题目数据: {ExamId}", examId);

                // 直接从数据库加载题目数据，避免循环依赖
                var questionsDict = await LoadQuestionsFromDatabaseAsync(examId);

                return questionsDict;
            });
    }

    /// <summary>
    /// 获取用户考试记录（带缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户考试记录</returns>
    public async Task<UserExamRecordCacheDto?> GetUserExamRecordWithCacheAsync(long examId, long userId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.UserRecord(examId, userId),
            async () =>
            {
                _logger.LogDebug("从数据库获取用户考试记录: ExamId={ExamId}, UserId={UserId}", examId, userId);

                // 直接查询学生信息，避免依赖其他服务
                var student = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return null;
                }

                // 查找考试记录
                var record = await _examRecordRepository.CreateQuery()
                    .AsNoTracking()
                    .Where(r => r.ExamSettingId == examId && r.StudentId == student.Id)
                    .OrderByDescending(r => r.StartTime)
                    .FirstOrDefaultAsync();

                if (record == null)
                {
                    return null;
                }

                return new UserExamRecordCacheDto
                {
                    RecordId = record.Id,
                    ScreenSwitchCount = record.ScreenSwitchCount,
                    StartTime = record.StartTime,
                    Status = record.Status
                };
            });
    }

    /// <summary>
    /// 获取用户已提交的答案（带缓存）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户答案列表</returns>
    public async Task<List<ClientExamAnswerDto>> GetSubmittedAnswersWithCacheAsync(long recordId, long userId)
    {
        // 使用分布式锁确保读取操作也是串行的，避免多实例下的缓存不一致
        var lockKey = $"exam_answer_cache_{recordId}_{userId}";
        var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
        
        _logger.LogInformation("获取用户答案缓存，缓存键: {CacheKey}, RecordId={RecordId}, UserId={UserId}",
            cacheKey.Key, recordId, userId);

        try
        {
            // 获取分布式锁，超时时间5秒（读取操作相对较快）
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(5)))
            {
                _logger.LogDebug("已获取答案缓存读取锁: {LockKey}", lockKey);

                return await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogDebug("从数据库获取用户答案: RecordId={RecordId}, UserId={UserId}", recordId, userId);
                        var answerEntities = await _answerRecordRepository.CreateQuery()
                            .AsNoTracking()
                            .Where(a => a.ExamRecordId == recordId)
                            .ToListAsync();
                        
                        var answers = answerEntities.Select(a => new ClientExamAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            Answer = a.Answer ?? string.Empty
                        }).ToList();
                        
                        _logger.LogDebug("从数据库加载答案并写入缓存: RecordId={RecordId}, 答案数={Count}", 
                            recordId, answers.Count);
                        
                        return answers;
                    });
            }
        }
        catch (TimeoutException ex)
        {
            // 获取锁超时，直接从数据库读取（不使用缓存）
            _logger.LogWarning(ex, "获取答案缓存读取锁超时: {LockKey}，直接从数据库读取", lockKey);
            
            var answerEntities = await _answerRecordRepository.CreateQuery()
                .AsNoTracking()
                .Where(a => a.ExamRecordId == recordId)
                .ToListAsync();
            
            return answerEntities.Select(a => new ClientExamAnswerDto
            {
                QuestionId = a.QuestionId,
                Answer = a.Answer ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户答案缓存失败: RecordId={RecordId}, UserId={UserId}，直接从数据库读取", 
                recordId, userId);
            
            // 出错时直接从数据库读取，确保数据可靠性
            var answerEntities = await _answerRecordRepository.CreateQuery()
                .AsNoTracking()
                .Where(a => a.ExamRecordId == recordId)
                .ToListAsync();
            
            return answerEntities.Select(a => new ClientExamAnswerDto
            {
                QuestionId = a.QuestionId,
                Answer = a.Answer ?? string.Empty
            }).ToList();
        }
    }

    /// <summary>
    /// 清除用户答案缓存
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    public async Task ClearUserAnswersCacheAsync(long recordId, long userId)
    {
        var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
        _logger.LogInformation("清除用户答案缓存，缓存键: {CacheKey}, RecordId={RecordId}, UserId={UserId}",
            cacheKey.Key, recordId, userId);
        await _cacheService.RemoveAsync(cacheKey);
        _logger.LogDebug("已清除用户答案缓存: RecordId={RecordId}, UserId={UserId}", recordId, userId);
    }

    /// <summary>
    /// 刷新用户答案缓存（主动更新缓存数据）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    public async Task RefreshUserAnswersCacheAsync(long recordId, long userId)
    {
        // 使用分布式锁确保刷新操作的原子性
        var lockKey = $"exam_answer_cache_{recordId}_{userId}";
        var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);

        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(5)))
            {
                _logger.LogDebug("已获取答案缓存刷新锁: {LockKey}", lockKey);

                // 先清除旧缓存
                await _cacheService.RemoveAsync(cacheKey);

                // 立即重新加载最新数据到缓存
                await _cacheService.GetOrSetAsync(
                    cacheKey,
                    async () =>
                    {
                        _logger.LogDebug("刷新缓存：从数据库获取最新用户答案: RecordId={RecordId}, UserId={UserId}", recordId, userId);
                        var answerEntities = await _answerRecordRepository.CreateQuery()
                            .AsNoTracking()
                            .Where(a => a.ExamRecordId == recordId)
                            .ToListAsync();
                        return answerEntities.Select(a => new ClientExamAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            Answer = a.Answer ?? string.Empty
                        }).ToList();
                    });

                _logger.LogDebug("已刷新用户答案缓存: RecordId={RecordId}, UserId={UserId}", recordId, userId);
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "获取答案缓存刷新锁超时: {LockKey}，清除缓存", lockKey);
            // 超时则清除缓存，下次读取时重新加载
            await _cacheService.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新用户答案缓存失败: RecordId={RecordId}, UserId={UserId}", recordId, userId);
            // 失败则清除缓存
            await _cacheService.RemoveAsync(cacheKey);
        }
    }

    /// <summary>
    /// 直接更新用户答案缓存（使用已有数据，无需查询数据库）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="newAnswers">新保存的答案</param>
    public async Task UpdateUserAnswersCacheAsync(long recordId, long userId, List<ClientExamAnswerDto> newAnswers)
    {
        // 使用分布式锁防止并发更新导致的缓存覆盖问题
        var lockKey = $"exam_answer_cache_{recordId}_{userId}";
        
        try
        {
            using (await _distributedLockProvider.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10)))
            {
                _logger.LogDebug("已获取答案缓存更新锁: {LockKey}", lockKey);

                // 获取当前缓存中的答案
                var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
                _logger.LogInformation("更新用户答案缓存，缓存键: {CacheKey}, RecordId={RecordId}, UserId={UserId}",
                    cacheKey.Key, recordId, userId);

                var existingAnswers = await _cacheService.GetAsync(cacheKey);

                List<ClientExamAnswerDto> updatedAnswers;

                if (existingAnswers != null && existingAnswers.Count > 0)
                {
                    // 如果缓存中有数据，则合并新答案
                    var answerDict = existingAnswers.ToDictionary(a => a.QuestionId, a => a);

                    // 更新新答案到字典中
                    foreach (var newAnswer in newAnswers)
                    {
                        if (answerDict.ContainsKey(newAnswer.QuestionId))
                        {
                            // 更新现有答案
                            answerDict[newAnswer.QuestionId].Answer = newAnswer.Answer;
                        }
                        else
                        {
                            // 添加新答案（理论上不应该发生，因为所有题目在考试开始时就创建了记录）
                            answerDict[newAnswer.QuestionId] = newAnswer;
                            _logger.LogWarning("发现缓存中缺少题目 {QuestionId} 的记录，已添加", newAnswer.QuestionId);
                        }
                    }

                    updatedAnswers = answerDict.Values.ToList();
                    _logger.LogDebug("合并缓存答案: RecordId={RecordId}, 原有答案数={ExistingCount}, 更新答案数={NewCount}, 合并后答案数={TotalCount}",
                        recordId, existingAnswers.Count, newAnswers.Count, updatedAnswers.Count);
                }
                else
                {
                    // 如果缓存中没有数据，需要从数据库获取完整答案列表
                    // 这种情况可能发生在缓存过期或首次访问时，需要确保获取所有题目的答案记录
                    _logger.LogDebug("缓存为空，从数据库获取完整答案列表: RecordId={RecordId}", recordId);
                    var answerEntities = await _answerRecordRepository.CreateQuery()
                        .AsNoTracking()
                        .Where(a => a.ExamRecordId == recordId)
                        .ToListAsync();

                    // 创建答案字典，包含所有题目
                    var answerDict = answerEntities.ToDictionary(
                        a => a.QuestionId,
                        a => new ClientExamAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            Answer = a.Answer ?? string.Empty
                        });

                    // 更新新保存的答案
                    foreach (var newAnswer in newAnswers)
                    {
                        if (answerDict.ContainsKey(newAnswer.QuestionId))
                        {
                            answerDict[newAnswer.QuestionId].Answer = newAnswer.Answer;
                        }
                    }

                    updatedAnswers = answerDict.Values.ToList();
                    _logger.LogDebug("从数据库重建缓存: RecordId={RecordId}, 数据库答案数={DbCount}, 更新答案数={NewCount}, 最终答案数={TotalCount}",
                        recordId, answerEntities.Count, newAnswers.Count, updatedAnswers.Count);
                }

                // 直接设置缓存
                await _cacheService.SetAsync(cacheKey, updatedAnswers);

                _logger.LogDebug("已直接更新用户答案缓存: RecordId={RecordId}, UserId={UserId}, 答案数={AnswerCount}",
                    recordId, userId, updatedAnswers.Count);
            }
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "获取答案缓存更新锁超时: {LockKey}，将清除缓存以确保数据一致性", lockKey);
            // 获取锁超时，清除缓存以确保下次从数据库读取最新数据
            var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
            await _cacheService.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接更新用户答案缓存失败: RecordId={RecordId}, UserId={UserId}", recordId, userId);
            // 更新失败时清除缓存，避免脏数据
            try
            {
                var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("已清除可能的脏缓存数据: RecordId={RecordId}, UserId={UserId}", recordId, userId);
            }
            catch
            {
                // 忽略清除缓存的异常
            }
        }
    }

    /// <summary>
    /// 预热考试缓存（仅预热即将开始的考试）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>预热任务</returns>
    public async Task WarmupExamCacheAsync(long examId)
    {
        _logger.LogInformation("开始预热考试缓存: {ExamId}", examId);

        try
        {
            // 首先检查考试是否即将开始
            var examSetting = await _context.ExamSettings
                .Where(e => e.Id == examId)
                .Select(e => new { e.StartTime, e.EndTime, e.Status })
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                _logger.LogWarning("考试不存在，跳过缓存预热: {ExamId}", examId);
                return;
            }

            var now = DateTime.UtcNow;

            // 检查考试是否即将开始（开始前预热窗口时间内）或正在进行中
            var isUpcoming = examSetting.StartTime <= now.Add(WarmupWindowBeforeStart) && examSetting.StartTime > now;
            var isOngoing = now >= examSetting.StartTime && now <= examSetting.EndTime;

            if (!isUpcoming && !isOngoing)
            {
                _logger.LogInformation("考试未在预热窗口期内，跳过缓存预热: {ExamId}, 开始时间: {StartTime}, 当前时间: {Now}",
                    examId, examSetting.StartTime, now);
                return;
            }

            _logger.LogInformation("考试在预热窗口期内，开始预热缓存: {ExamId}, 开始时间: {StartTime}",
                examId, examSetting.StartTime);

            // 预热考试基本信息
            await GetExamBasicInfoWithCacheAsync(examId);
            _logger.LogDebug("考试基本信息预热完成: {ExamId}", examId);

            // 预热考试题目数据
            await GetExamQuestionsDataWithCacheAsync(examId);
            _logger.LogDebug("考试题目数据预热完成: {ExamId}", examId);

            _logger.LogInformation("考试缓存预热完成: {ExamId}", examId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "考试缓存预热失败: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 批量预热即将开始的考试缓存
    /// </summary>
    /// <returns>预热任务</returns>
    public async Task WarmupUpcomingExamsCacheAsync()
    {
        _logger.LogInformation("开始批量预热即将开始的考试缓存");

        try
        {
            // ✅ 优先预热共享的可用考试列表缓存
            _logger.LogDebug("开始预热共享的可用考试列表缓存");
            await GetSharedAvailableExamsWithCacheAsync();
            _logger.LogDebug("共享的可用考试列表缓存预热完成");
            
            var now = DateTime.UtcNow;
            var warmupStartTime = now;
            var warmupEndTime = now.Add(WarmupWindowBeforeStart);

            // 查找即将开始的考试（开始时间在预热窗口内）
            var upcomingExams = await _context.ExamSettings
                .Where(e => e.Status == ExamSettingStatus.Published &&
                           e.StartTime >= warmupStartTime &&
                           e.StartTime <= warmupEndTime)
                .Select(e => new { e.Id, e.StartTime, e.Name })
                .ToListAsync();

            if (!upcomingExams.Any())
            {
                _logger.LogInformation("没有找到即将开始的考试，跳过批量预热");
                return;
            }

            _logger.LogInformation("找到 {Count} 个即将开始的考试，开始批量预热", upcomingExams.Count);

            var warmupTasks = upcomingExams.Select(async exam =>
            {
                try
                {
                    await WarmupExamCacheAsync(exam.Id);
                    _logger.LogDebug("考试缓存预热成功: {ExamId} - {ExamName}", exam.Id, exam.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "考试缓存预热失败: {ExamId} - {ExamName}", exam.Id, exam.Name);
                }
            });

            await Task.WhenAll(warmupTasks);
            _logger.LogInformation("批量预热即将开始的考试缓存完成，处理了 {Count} 个考试", upcomingExams.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量预热即将开始的考试缓存时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 清空考试缓存（清除所有与指定考试相关的缓存）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>清空任务</returns>
    public async Task ClearExamCacheAsync(long examId)
    {
        _logger.LogInformation("开始清空考试缓存: {ExamId}", examId);

        try
        {
            // 1. 清除考试基本信息缓存
            await _cacheService.RemoveAsync(new ExamCacheOptions.BasicInfo(examId));
            _logger.LogDebug("已清除考试基本信息缓存: {ExamId}", examId);

            // 2. 清除考试题目数据缓存
            await _cacheService.RemoveAsync(new ExamCacheOptions.Questions(examId));
            _logger.LogDebug("已清除考试题目数据缓存: {ExamId}", examId);

            // 3. 清除所有与该考试相关的用户记录和轻量信息缓存（通过模式匹配）
            // 格式：ExamCacheOptions_UserRecord_{ExamId}_{UserId} 和 ExamCacheOptions_LightInfo_{ExamId}_{StudentId}
            await _cacheService.RemoveByPatternAsync($"*UserRecord_{examId}_*");
            await _cacheService.RemoveByPatternAsync($"*LightInfo_{examId}_*");
            _logger.LogDebug("已清除考试用户记录和轻量信息缓存: {ExamId}", examId);

            // 4. ⚠️ 重要：清除所有学生的"可参加考试列表"缓存
            // 因为考试信息变更（时间、状态、权限等）可能影响所有学生的可用考试列表
            // AvailableExams 的 Key 格式：ExamCacheOptions_AvailableExams_{StudentId}
            await _cacheService.RemoveByPatternAsync("*AvailableExams_*");
            _logger.LogDebug("已清除所有学生的可参加考试列表缓存");

            // 5. ✅ 清除共享的可用考试列表缓存
            // 考试信息变更时，需要清除共享缓存以确保所有学生获取最新数据
            await ClearSharedAvailableExamsCacheAsync();
            _logger.LogDebug("已清除共享的可用考试列表缓存");

            // 6. 清除 OutputCache（如果使用了 ASP.NET Core OutputCache）
            // 注意：需要在 Controller 层面处理，或通过 IOutputCacheStore 清除
            // 这里只清除应用层缓存

            _logger.LogInformation("考试缓存清空完成: {ExamId}，已清除以下缓存：BasicInfo, Questions, UserRecord, LightInfo, AvailableExams, SharedAvailableExams", examId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空考试缓存时发生错误: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 清除用户考试记录缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="userId">用户ID</param>
    public async Task ClearUserExamRecordCacheAsync(long examId, long userId)
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.UserRecord(examId, userId));
        _logger.LogDebug("已清除用户考试记录缓存: ExamId={ExamId}, UserId={UserId}", examId, userId);
    }

    /// <summary>
    /// 从数据库加载题目数据（仅在缓存未命中时调用）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>题目数据字典</returns>
    private async Task<Dictionary<long, ClientExamQuestionDto>?> LoadQuestionsFromDatabaseAsync(long examId)
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
                _logger.LogWarning("考试不存在: {ExamId}", examId);
                return null;
            }

            // 加载题目版本信息
            foreach (var paperQuestion in examSetting.ExamPaper.ExamPaperQuestions)
            {
                await _context.Entry(paperQuestion)
                    .Reference(q => q.QuestionVersion)
                    .LoadAsync();
            }

            // 构建题目字典
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
                        TypeValue = (int)q.Question.Type,
                        SequenceNumber = 0  // 将由调用者设置
                    }
                );

            _logger.LogDebug("成功从数据库加载题目数据: ExamId={ExamId}, 题目数={Count}", examId, questionsDict.Count);
            return questionsDict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载题目数据时发生错误: ExamId={ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 从数据库加载考试基本信息（仅在缓存未命中时调用）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试基本信息</returns>
    private async Task<ExamBasicInfoCacheDto?> LoadExamBasicInfoFromDatabaseAsync(long examId)
    {
        try
        {
            var examSetting = await _context.ExamSettings
                .Include(e => e.ExamPaper)
                .Where(e => e.Id == examId)
                .FirstOrDefaultAsync();

            if (examSetting == null)
            {
                _logger.LogWarning("考试不存在: {ExamId}", examId);
                return null;
            }

            var basicInfo = new ExamBasicInfoCacheDto
            {
                Id = examSetting.Id,
                Name = examSetting.Name,
                Description = examSetting.Description,
                Duration = examSetting.Duration,
                StartTime = examSetting.StartTime,
                EndTime = examSetting.EndTime,
                TotalScore = examSetting.ExamPaper?.TotalScore ?? 0,
                AllowedScreenSwitchCount = examSetting.AllowedScreenSwitchCount,
                EnableViewResult = examSetting.EnableViewResult,
                MinExamTime = examSetting.MinExamTime,
                AllowedAttempts = examSetting.AllowedAttempts,
                EnableRandomQuestionOrder = examSetting.EnableRandomQuestionOrder,
                EnableRandomOptionOrder = examSetting.EnableRandomOptionOrder
            };

            _logger.LogDebug("成功从数据库加载考试基本信息: ExamId={ExamId}, Name={Name}", examId, basicInfo.Name);
            return basicInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载考试基本信息时发生错误: ExamId={ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 获取学生信息（带缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>学生信息</returns>
    public async Task<StudentDto?> GetStudentInfoWithCacheAsync(long userId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.StudentInfo(userId),
            async () =>
            {
                _logger.LogDebug("从数据库获取学生信息: UserId={UserId}", userId);

                // 直接从数据库加载学生信息，避免循环依赖
                var studentInfo = await LoadStudentInfoFromDatabaseAsync(userId);

                return studentInfo;
            });
    }

    /// <summary>
    /// 清除学生信息缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task ClearStudentInfoCacheAsync(long userId)
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.StudentInfo(userId));
        _logger.LogDebug("已清除学生信息缓存: UserId={UserId}", userId);
    }

    /// <summary>
    /// 获取客户端档案信息（带缓存）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>客户端档案信息</returns>
    public async Task<ClientProfileDto> GetClientProfileWithCacheAsync(long userId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.ClientProfile(userId),
            async () =>
            {
                _logger.LogDebug("从数据库获取客户端档案信息: UserId={UserId}", userId);

                // 直接从数据库加载客户端档案信息，避免循环依赖
                var clientProfile = await LoadClientProfileFromDatabaseAsync(userId);

                return clientProfile;
            });
    }

    /// <summary>
    /// 清除客户端档案信息缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task ClearClientProfileCacheAsync(long userId)
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.ClientProfile(userId));
        _logger.LogDebug("已清除客户端档案信息缓存: UserId={UserId}", userId);
    }

    /// <summary>
    /// 从数据库加载学生信息（仅在缓存未命中时调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>学生信息</returns>
    private async Task<StudentDto?> LoadStudentInfoFromDatabaseAsync(long userId)
    {
        try
        {
            var student = await _studentRepository.CreateQuery()
                .Include(s => s.StudentGroups)
                .ThenInclude(sg => sg.StudentGroup)
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                _logger.LogWarning("学生不存在: UserId={UserId}", userId);
                return null;
            }

            var studentDto = new StudentDto
            {
                Id = student.Id,
                UserId = student.UserId,
                Name = student.Name,
                IdNo = student.IdNo,
                Gender = student.Gender,
                AdmissionTicket = student.AdmissionTicket,
                StudentNumber = student.StudentNumber,
                PhoneNumber = student.PhoneNumber,
                IsActive = student.IsActive,
                CreatedAt = student.CreatedAt,
                StudentGroups = student.StudentGroups?.Select(sg => sg.StudentGroup.Name).ToList() ?? new List<string>(),
                StudentGroupIds = student.StudentGroups?.Select(sg => sg.StudentGroupId).ToList() ?? new List<long>()
            };

            _logger.LogDebug("成功从数据库加载学生信息: UserId={UserId}, Name={Name}", userId, studentDto.Name);
            return studentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载学生信息时发生错误: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 从数据库加载客户端档案信息（仅在缓存未命中时调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>客户端档案信息</returns>
    private async Task<ClientProfileDto> LoadClientProfileFromDatabaseAsync(long userId)
    {
        try
        {
            var student = await _studentRepository.CreateQuery()
                .Include(s => s.StudentGroups)
                .ThenInclude(sg => sg.StudentGroup)
                .Where(s => s.UserId == userId)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                throw new InvalidOperationException("未找到考生信息");
            }

            var clientProfile = new ClientProfileDto
            {
                Id = student.Id,
                UserId = student.UserId,
                Name = student.Name,
                StudentNumber = student.StudentNumber,
                IdNo = student.IdNo,
                Gender = student.Gender.GetDisplayName(),
                AdmissionTicket = student.AdmissionTicket,
                PhoneNumber = student.PhoneNumber,
                StudentGroups = student.StudentGroups?.Select(sg => sg.StudentGroup.Name).ToList() ?? new List<string>(),
                StudentGroupIds = student.StudentGroups?.Select(sg => sg.StudentGroupId).ToList() ?? new List<long>()
            };

            _logger.LogDebug("成功从数据库加载客户端档案信息: UserId={UserId}, Name={Name}, StudentGroupCount={Count}", 
                userId, clientProfile.Name, clientProfile.StudentGroupIds.Count);
            return clientProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载客户端档案信息时发生错误: UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试轻量信息（带缓存，用于倒计时页面）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    public async Task<ClientExamLightInfoDto?> GetExamLightInfoWithCacheAsync(long examId, long studentId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.LightInfo(examId, studentId),
            async () =>
            {
                _logger.LogDebug("从数据库获取考试轻量信息: ExamId={ExamId}, StudentId={StudentId}", examId, studentId);

                // 直接从数据库加载轻量信息，避免循环依赖
                var lightInfo = await LoadExamLightInfoFromDatabaseAsync(examId, studentId);

                return lightInfo;
            });
    }

    /// <summary>
    /// 清除考试轻量信息缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    public async Task ClearExamLightInfoCacheAsync(long examId, long studentId)
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.LightInfo(examId, studentId));
        _logger.LogDebug("已清除考试轻量信息缓存: ExamId={ExamId}, StudentId={StudentId}", examId, studentId);
    }

    /// <summary>
    /// 从数据库加载考试轻量信息（仅在缓存未命中时调用）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <returns>考试轻量信息</returns>
    private async Task<ClientExamLightInfoDto?> LoadExamLightInfoFromDatabaseAsync(long examId, long studentId)
    {
        try
        {
            // ✅ 性能优化：使用投影查询，只获取需要的字段，避免加载关联数据
            var examQuery = await _context.ExamSettings
                .Where(e => e.Id == examId)
                .Select(e => new 
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.Duration,
                    e.StartTime,
                    e.EndTime,
                    TotalScore = e.ExamPaper != null ? e.ExamPaper.TotalScore : 0,
                    QuestionCount = e.ExamPaper != null ? e.ExamPaper.ExamPaperQuestions.Count : 0,
                    // ✅ 直接在查询中检查权限，避免额外查询
                    HasPermission = !e.StudentGroups.Any() || 
                        e.StudentGroups.Any(g => g.StudentGroup.Students.Any(m => m.StudentId == studentId))
                })
                .FirstOrDefaultAsync();

            if (examQuery == null)
            {
                _logger.LogWarning("考试不存在: ExamId={ExamId}", examId);
                return null;
            }

            if (!examQuery.HasPermission)
            {
                _logger.LogWarning("学生无权参加此考试: ExamId={ExamId}, StudentId={StudentId}", examId, studentId);
                throw new UnauthorizedAccessException("无权参加此考试");
            }

            // 组装考试轻量信息
            var lightInfo = new ClientExamLightInfoDto
            {
                Id = examQuery.Id,
                Name = examQuery.Name,
                Description = examQuery.Description,
                Duration = examQuery.Duration,
                StartTime = examQuery.StartTime,
                EndTime = examQuery.EndTime,
                TotalScore = examQuery.TotalScore,
                QuestionCount = examQuery.QuestionCount,
                ServerTime = DateTime.UtcNow,
                Status = string.Empty, // 将由调用者设置
                CanStart = false // 将由调用者设置
            };

            _logger.LogDebug("成功从数据库加载考试轻量信息: ExamId={ExamId}, Name={Name}", examId, lightInfo.Name);
            return lightInfo;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "从数据库加载考试轻量信息时发生错误: ExamId={ExamId}, StudentId={StudentId}", examId, studentId);
            throw;
        }
    }
    
    /// <summary>
    /// 获取考试记录（带缓存，轻量级）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录缓存DTO</returns>
    public async Task<ExamRecordCacheDto?> GetExamRecordWithCacheAsync(long recordId)
    {
        return await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.ExamRecordCache(recordId),
            async () =>
            {
                _logger.LogDebug("从数据库获取考试记录（轻量级）: RecordId={RecordId}", recordId);
                
                // 直接从数据库加载考试记录，避免循环依赖
                var examRecord = await LoadExamRecordFromDatabaseAsync(recordId);
                
                return examRecord;
            });
    }
    
    /// <summary>
    /// 清除考试记录缓存
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    public async Task ClearExamRecordCacheAsync(long recordId)
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.ExamRecordCache(recordId));
        _logger.LogDebug("已清除考试记录缓存: RecordId={RecordId}", recordId);
    }
    
    /// <summary>
    /// 从数据库加载考试记录（仅在缓存未命中时调用，轻量级查询）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <returns>考试记录缓存DTO</returns>
    private async Task<ExamRecordCacheDto?> LoadExamRecordFromDatabaseAsync(long recordId)
    {
        try
        {
            // 仅查询答题验证所需的关键字段，不关联查询其他表，提高性能
            var examRecord = await _examRecordRepository.CreateQuery()
                .Where(r => r.Id == recordId)
                .Select(r => new ExamRecordCacheDto
                {
                    Id = r.Id,
                    StudentId = r.StudentId,
                    Status = r.Status,
                    ExamSettingId = r.ExamSettingId
                })
                .FirstOrDefaultAsync();
            
            if (examRecord == null)
            {
                _logger.LogWarning("考试记录不存在: RecordId={RecordId}", recordId);
                return null;
            }
            
            _logger.LogDebug("成功从数据库加载考试记录（轻量级）: RecordId={RecordId}, StudentId={StudentId}, Status={Status}", 
                recordId, examRecord.StudentId, examRecord.Status);
            return examRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载考试记录（轻量级）时发生错误: RecordId={RecordId}", recordId);
            throw;
        }
    }
    
    /// <summary>
    /// 获取共享的可用考试列表（带缓存）
    /// </summary>
    /// <returns>可用考试列表</returns>
    public async Task<List<SharedAvailableExamDto>> GetSharedAvailableExamsWithCacheAsync()
    {
        var cacheKey = new ExamCacheOptions.SharedAvailableExams();
        
        // 先尝试从缓存获取
        var cachedExams = await _cacheService.GetAsync(cacheKey);
        if (cachedExams != null)
        {
            _logger.LogDebug("从缓存获取到共享的可用考试列表，数量: {Count}", cachedExams.Count);
            return cachedExams;
        }
        
        // 缓存未命中，从数据库加载
        _logger.LogDebug("缓存未命中，从数据库获取共享的可用考试列表");
        var exams = await LoadSharedAvailableExamsFromDatabaseAsync();
        
        // ✅ 关键优化：只有当结果不为空时才缓存
        // 空结果不缓存，避免在压测或系统初始化时缓存空数组
        // 导致后续即使有可用考试也查询不到
        if (exams.Count > 0)
        {
            await _cacheService.SetAsync(cacheKey, exams);
            _logger.LogDebug("已缓存共享的可用考试列表，数量: {Count}", exams.Count);
        }
        else
        {
            _logger.LogWarning("数据库中没有可用的考试，不缓存空结果，确保下次查询时重新加载");
        }
        
        return exams;
    }
    
    /// <summary>
    /// 从数据库加载共享的可用考试列表（仅在缓存未命中时调用）
    /// </summary>
    /// <returns>可用考试列表</returns>
    private async Task<List<SharedAvailableExamDto>> LoadSharedAvailableExamsFromDatabaseAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var previewTime = now.AddMinutes(30); // 开考前30分钟可见
            
            _logger.LogDebug("开始从数据库加载共享的可用考试列表，当前时间: {Now}", now);
            
            // 查询已发布且正在进行或即将开始的考试
            var exams = await _context.ExamSettings
                .Where(e => e.Status == ExamSettingStatus.Published)
                .Where(e =>
                    // 正在进行中的考试或即将开始的考试（开考前半小时）
                    ((e.StartTime <= now && e.EndTime >= now) ||
                     (e.StartTime > now && e.StartTime <= previewTime))
                )
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Description,
                    e.StartTime,
                    e.EndTime,
                    e.Duration,
                    TotalScore = e.ExamPaper != null ? e.ExamPaper.TotalScore : 0,
                    // 获取考生组ID列表
                    StudentGroupIds = e.StudentGroups.Select(sg => sg.StudentGroupId).ToList()
                })
                .ToListAsync();
            
            // 转换为 SharedAvailableExamDto
            var result = exams.Select(e => new SharedAvailableExamDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
                TotalScore = e.TotalScore,
                StudentGroupIds = e.StudentGroupIds,
                IsOpenToAll = !e.StudentGroupIds.Any() // 如果没有考生组限制，则对所有人开放
            }).ToList();
            
            _logger.LogDebug("成功从数据库加载共享的可用考试列表，考试数量: {Count}", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载共享的可用考试列表时发生错误");
            throw;
        }
    }
    
    /// <summary>
    /// 清除共享的可用考试列表缓存
    /// </summary>
    public async Task ClearSharedAvailableExamsCacheAsync()
    {
        await _cacheService.RemoveAsync(new ExamCacheOptions.SharedAvailableExams());
        _logger.LogDebug("已清除共享的可用考试列表缓存");
    }
}
