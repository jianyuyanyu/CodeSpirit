using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.Caching.Models;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ExamApi.Services.Implementations;

/// <summary>
/// 考试记录预生成服务实现
/// </summary>
public class ExamRecordPreGenerationService : IExamRecordPreGenerationService, IScopedDependency
{
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly IRepository<ExamAnswerRecord> _answerRecordRepository;
    private readonly IRepository<StudentGroupMapping> _studentGroupMappingRepository;
    private readonly IRepository<ExamSettingStudentGroup> _examSettingStudentGroupRepository;
    private readonly IExamCacheService _examCacheService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ExamRecordPreGenerationService> _logger;
    
    // 配置常量
    /// <summary>
    /// 批次大小
    /// </summary>
    private const int BATCH_SIZE = 50;
    
    /// <summary>
    /// 批次间延迟时间（毫秒），用于限制生成速度，避免对CPU和数据库造成过大压力
    /// </summary>
    private const int DELAY_BETWEEN_BATCHES_MS = 200;
    
    /// <summary>
    /// 开考前停止预生成的时间阈值（分钟）
    /// </summary>
    private const int STOP_BEFORE_EXAM_START_MINUTES = 5;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamRecordPreGenerationService(
        IRepository<ExamRecord> examRecordRepository,
        IRepository<ExamAnswerRecord> answerRecordRepository,
        IRepository<StudentGroupMapping> studentGroupMappingRepository,
        IRepository<ExamSettingStudentGroup> examSettingStudentGroupRepository,
        IExamCacheService examCacheService,
        ICacheService cacheService,
        ILogger<ExamRecordPreGenerationService> logger)
    {
        _examRecordRepository = examRecordRepository;
        _answerRecordRepository = answerRecordRepository;
        _studentGroupMappingRepository = studentGroupMappingRepository;
        _examSettingStudentGroupRepository = examSettingStudentGroupRepository;
        _examCacheService = examCacheService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// 为指定考试预生成所有学生的考试记录
    /// </summary>
    public async Task<PreGenerationResult> PreGenerateExamRecordsAsync(long examId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("开始为考试 {ExamId} 预生成考试记录", examId);
        
        // 1. 获取考试基本信息和题目数据（使用缓存）
        var examBasicInfo = await _examCacheService.GetExamBasicInfoWithCacheAsync(examId);
        if (examBasicInfo == null)
        {
            throw new ArgumentException($"考试不存在：{examId}");
        }
        
        _logger.LogInformation("考试名称: {Name}, 考试ID: {ExamId}", 
            examBasicInfo.Name, examId);
        
        // 2. 获取学生分组列表
        var studentGroupIds = await _examSettingStudentGroupRepository.CreateQuery()
            .Where(esg => esg.ExamSettingId == examId)
            .Select(esg => esg.StudentGroupId)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        if (!studentGroupIds.Any())
        {
            _logger.LogWarning("考试 {ExamId} 没有分配学生分组，无需预生成", examId);
            return new PreGenerationResult();
        }
        
        _logger.LogInformation("获取到 {Count} 个学生分组", studentGroupIds.Count);
        
        // 3. 获取所有学生ID
        var studentIds = await _studentGroupMappingRepository.CreateQuery()
            .Where(m => studentGroupIds.Contains(m.StudentGroupId))
            .Select(m => m.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("获取到 {Count} 名学生需要预生成记录", studentIds.Count);
        
        if (!studentIds.Any())
        {
            _logger.LogWarning("考试 {ExamId} 没有学生，无需预生成", examId);
            return new PreGenerationResult();
        }
        
        // 4. 分批预生成
        var batches = studentIds.Chunk(BATCH_SIZE).ToList();
        var totalResult = new PreGenerationResult();
        
        _logger.LogInformation("开始分批预生成，每批 {BatchSize} 名学生，共 {BatchCount} 批，批次间延迟 {DelayMs}ms", 
            BATCH_SIZE, batches.Count, DELAY_BETWEEN_BATCHES_MS);
        
        var examStartTime = examBasicInfo.StartTime;
        var stopThreshold = TimeSpan.FromMinutes(STOP_BEFORE_EXAM_START_MINUTES);
        
        for (int i = 0; i < batches.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // ✅ 检查1：开考前5分钟停止预生成
            if (examStartTime.HasValue)
            {
                var now = DateTime.UtcNow;
                var timeUntilStart = examStartTime.Value - now;
                
                // 如果考试已经开始，立即停止
                if (timeUntilStart <= TimeSpan.Zero)
                {
                    var remainingCount = studentIds.Count - (totalResult.SuccessCount + totalResult.FailedCount + totalResult.SkippedCount);
                    _logger.LogWarning("⚠️ 考试已经开始，停止预生成。已处理: {Processed}/{Total}，剩余: {Remaining} 名学生未处理",
                        totalResult.SuccessCount + totalResult.FailedCount + totalResult.SkippedCount,
                        studentIds.Count,
                        remainingCount);
                    break;
                }
                
                // 如果距离开始时间不足5分钟，停止预生成
                if (timeUntilStart <= stopThreshold)
                {
                    var remainingCount = studentIds.Count - (totalResult.SuccessCount + totalResult.FailedCount + totalResult.SkippedCount);
                    _logger.LogWarning("⚠️ 距离考试开始时间不足 {Minutes} 分钟，停止预生成。已处理: {Processed}/{Total}，剩余: {Remaining} 名学生未处理",
                        STOP_BEFORE_EXAM_START_MINUTES,
                        totalResult.SuccessCount + totalResult.FailedCount + totalResult.SkippedCount,
                        studentIds.Count,
                        remainingCount);
                    break;
                }
                
                // 记录剩余时间（每10批记录一次，避免日志过多）
                if (i % 10 == 0)
                {
                    _logger.LogDebug("距离考试开始还有 {Minutes:F1} 分钟，继续预生成...", timeUntilStart.TotalMinutes);
                }
            }
            
            var batch = batches[i];
            _logger.LogInformation("正在处理第 {Current}/{Total} 批 ({Count} 名学生)...", 
                i + 1, batches.Count, batch.Count());
            
            var batchResult = await PreGenerateBatchAsync(examId, batch, attemptNumber: 1, cancellationToken);
            
            totalResult.SuccessCount += batchResult.SuccessCount;
            totalResult.FailedCount += batchResult.FailedCount;
            totalResult.SkippedCount += batchResult.SkippedCount;
            totalResult.FailedStudentIds.AddRange(batchResult.FailedStudentIds);
            
            _logger.LogInformation("第 {Current}/{Total} 批完成 - 成功: {Success}, 失败: {Failed}, 跳过: {Skipped}", 
                i + 1, batches.Count, batchResult.SuccessCount, batchResult.FailedCount, batchResult.SkippedCount);
            
            // ✅ 检查2：批次间延迟，限制生成速度（最后一批不需要延迟）
            if (i < batches.Count - 1)
            {
                await Task.Delay(DELAY_BETWEEN_BATCHES_MS, cancellationToken);
            }
        }
        
        _logger.LogInformation("========================================");
        _logger.LogInformation("预生成完成 - 总学生数: {Total}, 成功: {Success}, 失败: {Failed}, 跳过: {Skipped}", 
            studentIds.Count, totalResult.SuccessCount, totalResult.FailedCount, totalResult.SkippedCount);
        _logger.LogInformation("========================================");
        
        return totalResult;
    }
    
    /// <summary>
    /// 为指定学生批量预生成考试记录
    /// </summary>
    public async Task<PreGenerationResult> PreGenerateBatchAsync(
        long examId, 
        IEnumerable<long> studentIds, 
        int attemptNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var result = new PreGenerationResult();
        
        // 获取考试信息和题目数据（使用缓存）
        var examBasicInfo = await _examCacheService.GetExamBasicInfoWithCacheAsync(examId);
        var questionsData = await _examCacheService.GetExamQuestionsDataWithCacheAsync(examId);
        
        if (questionsData == null || !questionsData.Any())
        {
            _logger.LogError("考试 {ExamId} 没有题目数据，无法预生成", examId);
            throw new InvalidOperationException($"考试 {examId} 没有题目数据");
        }
        
        // 注意：租户上下文已通过ICurrentUser设置，MultiTenantDbContext会自动从ICurrentUser.TenantId获取
        // 因此创建实体时不需要显式设置TenantId，框架会自动设置
        
        await _examRecordRepository.ExecuteInTransactionAsync(async () =>
        {
            foreach (var studentId in studentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    // 检查是否已存在记录（防止重复预生成）
                    var exists = await _examRecordRepository.CreateQuery()
                        .AnyAsync(r => 
                            r.ExamSettingId == examId && 
                            r.StudentId == studentId && 
                            r.AttemptNumber == attemptNumber);
                    
                    if (exists)
                    {
                        result.SkippedCount++;
                        _logger.LogDebug("跳过已存在的记录: 学生ID={StudentId}, 考试ID={ExamId}", studentId, examId);
                        continue;
                    }
                    
                    // 创建考试记录（NotStarted状态）
                    // TenantId会由MultiTenantDbContext在SaveChanges时自动设置（从ICurrentUser.TenantId获取）
                    var examRecord = new ExamRecord
                    {
                        ExamSettingId = examId,
                        StudentId = studentId,
                        AttemptNumber = attemptNumber,
                        Status = ExamRecordStatus.NotStarted,  // 关键：预生成状态
                        IpAddress = string.Empty,
                        DeviceInfo = string.Empty
                        // ⚠️ StartTime 在实际开始时设置
                        // ⚠️ TenantId 由MultiTenantDbContext自动设置
                    };
                    
                    await _examRecordRepository.AddAsync(examRecord);
                    
                    // 预生成答题记录（含题目顺序）
                    var questionsList = questionsData.Values.ToList();
                    
                    // 题目乱序处理
                    if (examBasicInfo.EnableRandomQuestionOrder)
                    {
                        var random = new Random(Guid.NewGuid().GetHashCode());
                        // 先随机打乱，再按题型排序，确保同类型题目相对顺序一致
                        questionsList = questionsList.OrderBy(q => random.Next()).OrderBy(q => q.TypeValue).ToList();
                    }
                    else
                    {
                        // 不启用乱序时，按题型排序
                        questionsList = questionsList.OrderBy(q => q.TypeValue).ToList();
                    }
                    
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
                            IsMarked = false,
                            QuestionScore = question.Score
                            // TenantId会由MultiTenantDbContext在SaveChanges时自动设置（从ICurrentUser.TenantId获取）
                        });
                    }
                    
                    await _answerRecordRepository.AddRangeAsync(answerRecords);
                    
                    result.SuccessCount++;
                    _logger.LogDebug("✅ 成功预生成: 学生ID={StudentId}, 记录ID={RecordId}, 题目数={QuestionCount}",
                        studentId, examRecord.Id, answerRecords.Count);
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedStudentIds.Add(studentId);
                    _logger.LogError(ex, "预生成失败: 学生ID={StudentId}, 考试ID={ExamId}", studentId, examId);
                }
            }
        });
        
        return result;
    }
}

