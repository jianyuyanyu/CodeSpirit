using CodeSpirit.Core;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ScheduledTasks.Services;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ExamApi.Tasks;

/// <summary>
/// 考试记录定时预生成任务处理器
/// 每天凌晨1点执行，为所有已发布且尚未开始的考试预生成记录
/// </summary>
public class ExamRecordScheduledPreGenerationTaskHandler : ITaskHandler
{
    private readonly ExamDbContext _dbContext;
    private readonly IRepository<ExamSetting> _examSettingRepository;
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ExamRecordScheduledPreGenerationTaskHandler> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="examSettingRepository">考试设置仓储</param>
    /// <param name="examRecordRepository">考试记录仓储</param>
    /// <param name="serviceScopeFactory">服务作用域工厂</param>
    /// <param name="logger">日志记录器</param>
    public ExamRecordScheduledPreGenerationTaskHandler(
        ExamDbContext dbContext,
        IRepository<ExamSetting> examSettingRepository,
        IRepository<ExamRecord> examRecordRepository,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ExamRecordScheduledPreGenerationTaskHandler> logger)
    {
        _dbContext = dbContext;
        _examSettingRepository = examSettingRepository;
        _examRecordRepository = examRecordRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行定时预生成任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("考试记录定时预生成任务开始执行");
        _logger.LogInformation("========================================");
        
        try
        {
            var now = DateTime.UtcNow;
            
            // 1. 查询所有已发布且尚未开始的考试（禁用租户过滤器，因为这是系统级别的定时任务）
            var publishedExams = await _dbContext.WithoutMultiTenantFilterAsync(async () =>
            {
                return await _examSettingRepository.CreateQuery()
                    .Where(es => es.Status == ExamSettingStatus.Published && es.StartTime > now)
                    .OrderBy(es => es.StartTime)
                    .ToListAsync(cancellationToken);
            });
            
            if (!publishedExams.Any())
            {
                _logger.LogInformation("没有需要预生成的考试（已发布且尚未开始）");
                return "没有需要预生成的考试";
            }
            
            _logger.LogInformation("找到 {Count} 个已发布且尚未开始的考试", publishedExams.Count);
            
            // 2. 检查每个考试是否已预生成，并为未预生成的考试执行预生成
            int successCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            var failedExamIds = new List<long>();
            
            foreach (var exam in publishedExams)
            {
                try
                {
                    // 检查是否已预生成（通过检查是否存在NotStarted状态的ExamRecord）
                    // 注意：需要在禁用租户过滤器的上下文中检查，但需要确保查询的是该考试所属租户的记录
                    var hasPreGenerated = await _dbContext.WithoutMultiTenantFilterAsync(async () =>
                    {
                        return await _examRecordRepository.CreateQuery()
                            .AnyAsync(er => er.ExamSettingId == exam.Id 
                                && er.Status == ExamRecordStatus.NotStarted 
                                && er.TenantId == exam.TenantId, cancellationToken);
                    });
                    
                    if (hasPreGenerated)
                    {
                        _logger.LogInformation("考试 {ExamId} ({ExamName}) 已预生成，跳过", exam.Id, exam.Name);
                        skippedCount++;
                        continue;
                    }
                    
                    _logger.LogInformation("开始为考试 {ExamId} ({ExamName}) 预生成记录，租户ID: {TenantId}", exam.Id, exam.Name, exam.TenantId);
                    
                    // 为每个考试创建独立的服务作用域，并设置租户上下文
                    // 这样后续的所有操作（查询、创建）都会自动应用正确的租户上下文
                    // 注意：ICurrentUser是Scoped服务，作用域销毁时实例会被销毁，无需手动重置
                    PreGenerationResult result;
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        // 设置租户上下文到ICurrentUser
                        var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();
                        if (currentUser is ISettableCurrentUser settableCurrentUser)
                        {
                            settableCurrentUser.SetTenantId(exam.TenantId);
                            _logger.LogDebug("已设置租户上下文: TenantId={TenantId}", exam.TenantId);
                        }
                        else
                        {
                            _logger.LogWarning("无法设置租户上下文，ICurrentUser不支持设置租户ID");
                        }
                        
                        // 从作用域中获取预生成服务，确保使用正确的租户上下文
                        var scopedPreGenerationService = scope.ServiceProvider.GetRequiredService<IExamRecordPreGenerationService>();
                        
                        // 执行预生成（在租户上下文中执行，数据筛选器和TenantId设置都会自动应用）
                        result = await scopedPreGenerationService.PreGenerateExamRecordsAsync(exam.Id, cancellationToken);
                        
                        // 作用域销毁时，ICurrentUser实例会被自动销毁，租户上下文也会自动清理
                    }
                    
                    if (result.FailedCount > 0)
                    {
                        _logger.LogWarning("考试 {ExamId} 预生成部分失败 - 成功: {Success}, 失败: {Failed}, 跳过: {Skipped}", 
                            exam.Id, result.SuccessCount, result.FailedCount, result.SkippedCount);
                    }
                    else
                    {
                        _logger.LogInformation("考试 {ExamId} 预生成完成 - 成功: {Success}, 跳过: {Skipped}", 
                            exam.Id, result.SuccessCount, result.SkippedCount);
                    }
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "考试 {ExamId} ({ExamName}) 预生成失败", exam.Id, exam.Name);
                    failedCount++;
                    failedExamIds.Add(exam.Id);
                }
            }
            
            var message = $"定时预生成完成 - 总计: {publishedExams.Count}, 成功: {successCount}, 跳过: {skippedCount}, 失败: {failedCount}";
            
            if (failedCount > 0)
            {
                _logger.LogWarning("部分考试预生成失败，失败的考试ID: {FailedIds}", string.Join(", ", failedExamIds));
            }
            
            _logger.LogInformation(message);
            _logger.LogInformation("========================================");
            
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "考试记录定时预生成任务执行失败");
            throw;
        }
    }
}

