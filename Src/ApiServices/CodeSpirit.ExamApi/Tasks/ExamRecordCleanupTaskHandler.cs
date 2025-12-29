using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ScheduledTasks.Services;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.Tasks;

/// <summary>
/// 考试记录垃圾数据清理任务处理器
/// </summary>
public class ExamRecordCleanupTaskHandler : ITaskHandler
{
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly IExamRecordPreGenerationService _preGenerationService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ExamRecordCleanupTaskHandler> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ExamRecordCleanupTaskHandler(
        IRepository<ExamRecord> examRecordRepository,
        IExamRecordPreGenerationService preGenerationService,
        ICacheService cacheService,
        ILogger<ExamRecordCleanupTaskHandler> logger)
    {
        _examRecordRepository = examRecordRepository;
        _preGenerationService = preGenerationService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行清理任务
    /// </summary>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("开始执行考试记录垃圾数据清理任务");
        
        try
        {
            // 解析参数（清理多少天前的数据，默认7天）
            var cleanupDays = 7;
            if (!string.IsNullOrEmpty(parameters))
            {
                var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters);
                if (paramDict?.ContainsKey("cleanupDays") == true)
                {
                    cleanupDays = Convert.ToInt32(paramDict["cleanupDays"]);
                }
            }
            
            var threshold = DateTime.UtcNow.AddDays(-cleanupDays);
            
            _logger.LogInformation("清理条件: 状态=NotStarted + 考试已结束 + 创建时间早于 {Threshold}", threshold);
            
            // 查询需要清理的记录
            var query = _examRecordRepository.CreateQuery()
                .Include(r => r.ExamSetting)
                .Where(r => 
                    r.Status == ExamRecordStatus.NotStarted &&  // 未开始的
                    r.ExamSetting.EndTime < DateTime.UtcNow &&  // 考试已结束
                    r.CreatedAt < threshold);                    // 创建时间超过阈值
            
            var recordsToDelete = await query.ToListAsync(cancellationToken);
            
            if (!recordsToDelete.Any())
            {
                _logger.LogInformation("无需清理的垃圾数据");
                _logger.LogInformation("========================================");
                return "无垃圾数据需要清理";
            }
            
            _logger.LogInformation("找到 {Count} 条垃圾数据，开始清理...", recordsToDelete.Count);
            
            // 批量删除数据库记录（答题记录会级联删除）
            await _examRecordRepository.DeleteRangeAsync(recordsToDelete);
            
            // ✅ 关键优化：同步删除缓存
            var cacheDeletedCount = 0;
            foreach (var record in recordsToDelete)
            {
                try
                {
                    var cacheKey = _preGenerationService.GetPreGeneratedRecordCacheKey(
                        record.ExamSettingId, 
                        record.StudentId, 
                        record.AttemptNumber);
                    
                    await _cacheService.RemoveAsync(cacheKey);
                    cacheDeletedCount++;
                    
                    _logger.LogDebug("已删除缓存: {CacheKey}", cacheKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除缓存失败: 记录ID={RecordId}", record.Id);
                }
            }
            
            var result = $"清理完成 - 删除数据库记录: {recordsToDelete.Count} 条, 删除缓存: {cacheDeletedCount} 个";
            _logger.LogInformation(result);
            _logger.LogInformation("========================================");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "垃圾数据清理任务执行失败");
            throw;
        }
    }
}

