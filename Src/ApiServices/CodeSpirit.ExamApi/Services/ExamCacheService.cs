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
    public ExamCacheService(
        ICacheService cacheService,
        ICacheWarmupService warmupService,
        IRepository<ExamRecord> examRecordRepository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<Student> studentRepository,
        ILogger<ExamCacheService> logger,
        ExamDbContext context)
    {
        _cacheService = cacheService;
        _warmupService = warmupService;
        _examRecordRepository = examRecordRepository;
        _answerRecordRepository = answerRecordRepository;
        _studentRepository = studentRepository;
        _logger = logger;
        _context = context;
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
                    .Where(s => s.UserId == userId)
                    .FirstOrDefaultAsync();
                    
                if (student == null)
                {
                    return null;
                }
                
                // 查找考试记录
                var record = await _examRecordRepository.CreateQuery()
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
                    ScreenSwitchCount = record.ScreenSwitchCount
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
        var cacheKey = new ExamCacheOptions.UserAnswers(recordId, userId);
        _logger.LogInformation("获取用户答案缓存，缓存键: {CacheKey}, RecordId={RecordId}, UserId={UserId}", 
            cacheKey.Key, recordId, userId);
            
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("从数据库获取用户答案: RecordId={RecordId}, UserId={UserId}", recordId, userId);
                var answerEntities = await _answerRecordRepository.CreateQuery()
                    .Where(a => a.ExamRecordId == recordId)
                    .ToListAsync();
                return answerEntities.Select(a => new ClientExamAnswerDto
                {
                    QuestionId = a.QuestionId,
                    Answer = a.Answer ?? string.Empty
                }).ToList();
            });
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
        // 先清除旧缓存
        await _cacheService.RemoveAsync(new ExamCacheOptions.UserAnswers(recordId, userId));
        
        // 立即重新加载最新数据到缓存
        await _cacheService.GetOrSetAsync(
            new ExamCacheOptions.UserAnswers(recordId, userId),
            async () =>
            {
                _logger.LogDebug("刷新缓存：从数据库获取最新用户答案: RecordId={RecordId}, UserId={UserId}", recordId, userId);
                var answerEntities = await _answerRecordRepository.CreateQuery()
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

    /// <summary>
    /// 直接更新用户答案缓存（使用已有数据，无需查询数据库）
    /// </summary>
    /// <param name="recordId">考试记录ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="newAnswers">新保存的答案</param>
    public async Task UpdateUserAnswersCacheAsync(long recordId, long userId, List<ClientExamAnswerDto> newAnswers)
    {
        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "直接更新用户答案缓存失败: RecordId={RecordId}, UserId={UserId}", recordId, userId);
            throw;
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
    /// 清空考试缓存
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>清空任务</returns>
    public async Task ClearExamCacheAsync(long examId)
    {
        _logger.LogInformation("开始清空考试缓存: {ExamId}", examId);

        var basicInfoKey = new ExamCacheOptions.BasicInfo(examId);
        var questionsKey = new ExamCacheOptions.Questions(examId);

        var keysToRemove = new[]
        {
            basicInfoKey.Key,
            questionsKey.Key
        };

        await _cacheService.RemoveManyAsync(keysToRemove);
        
        // 也可以按模式清除（如果支持的话）
        await _cacheService.RemoveByPatternAsync($"*exam:{examId}*");
        
        _logger.LogInformation("考试缓存清空完成: {ExamId}", examId);
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
                StudentGroups = student.StudentGroups?.Select(sg => sg.StudentGroup.Name).ToList() ?? new List<string>()
            };

            _logger.LogDebug("成功从数据库加载客户端档案信息: UserId={UserId}, Name={Name}", userId, clientProfile.Name);
            return clientProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库加载客户端档案信息时发生错误: UserId={UserId}", userId);
            throw;
        }
    }

}
