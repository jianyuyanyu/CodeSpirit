using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Data.Models;
using CodeSpirit.ExamApi.Data.Models.Enums;
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
    private readonly ExamDbContext _dbContext;
    private readonly IRepository<ExamRecord> _examRecordRepository;
    private readonly ILogger<ExamRecordCleanupTaskHandler> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="examRecordRepository">考试记录仓储</param>
    /// <param name="logger">日志记录器</param>
    public ExamRecordCleanupTaskHandler(
        ExamDbContext dbContext,
        IRepository<ExamRecord> examRecordRepository,
        ILogger<ExamRecordCleanupTaskHandler> logger)
    {
        _dbContext = dbContext;
        _examRecordRepository = examRecordRepository;
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
            
            // 查询需要清理的记录（禁用租户过滤器，因为这是系统级别的定时任务）
            var recordsToDelete = await _dbContext.WithoutMultiTenantFilterAsync(async () =>
            {
                var query = _dbContext.Set<ExamRecord>()
                    .Include(r => r.ExamSetting)
                    .Where(r => 
                        r.Status == ExamRecordStatus.NotStarted &&  // 未开始的
                        r.ExamSetting.EndTime < DateTime.UtcNow &&  // 考试已结束
                        r.CreatedAt < threshold);                    // 创建时间超过阈值
                
                return await query.ToListAsync(cancellationToken);
            });
            
            if (!recordsToDelete.Any())
            {
                _logger.LogInformation("无需清理的垃圾数据");
                _logger.LogInformation("========================================");
                return "无垃圾数据需要清理";
            }
            
            _logger.LogInformation("找到 {Count} 条垃圾数据（跨所有租户），开始清理...", recordsToDelete.Count);
            
            // 按租户分组显示统计信息
            var groupedByTenant = recordsToDelete.GroupBy(r => r.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToList();
            
            foreach (var group in groupedByTenant)
            {
                _logger.LogInformation("租户 {TenantId}: {Count} 条记录", group.TenantId, group.Count);
            }
            
            // 批量删除数据库记录（答题记录会级联删除）
            // 注意：删除操作需要在禁用租户过滤器的上下文中执行
            await _dbContext.WithoutMultiTenantFilterAsync(async () =>
            {
                // 直接使用 DbContext 操作以避免并发问题
                _dbContext.Set<ExamRecord>().RemoveRange(recordsToDelete);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Task.CompletedTask;
            });
            
            var result = $"清理完成 - 删除数据库记录: {recordsToDelete.Count} 条 (跨 {groupedByTenant.Count} 个租户)";
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

