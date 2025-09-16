using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批日志服务实现
/// </summary>
public class ApprovalLogService : IApprovalLogService
{
    private readonly IRepository<ApprovalLog> _logRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ApprovalLogService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logRepository">审批日志仓储</param>
    /// <param name="mapper">映射器</param>
    /// <param name="logger">日志记录器</param>
    public ApprovalLogService(
        IRepository<ApprovalLog> logRepository,
        IMapper mapper,
        ILogger<ApprovalLogService> logger)
    {
        _logRepository = logRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 记录审批日志
    /// </summary>
    /// <param name="log">审批日志</param>
    /// <returns>记录结果</returns>
    public async Task<bool> LogAsync(ApprovalLog log)
    {
        try
        {
            await _logRepository.AddAsync(log);

            _logger.LogDebug("审批日志记录成功: 实例ID={InstanceId}, 操作类型={LogType}, 操作人={OperatorId}",
                log.ApprovalInstanceId, log.LogType, log.OperatorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录审批日志失败: 实例ID={InstanceId}, 操作类型={LogType}",
                log.ApprovalInstanceId, log.LogType);
            return false;
        }
    }

    /// <summary>
    /// 批量记录审批日志
    /// </summary>
    /// <param name="logs">审批日志列表</param>
    /// <returns>记录结果</returns>
    public async Task<bool> LogBatchAsync(List<ApprovalLog> logs)
    {
        if (!logs.Any())
            return true;

        try
        {
            await _logRepository.AddRangeAsync(logs);

            _logger.LogDebug("批量审批日志记录成功: 数量={Count}", logs.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量记录审批日志失败: 数量={Count}", logs.Count);
            return false;
        }
    }

    /// <summary>
    /// 获取审批日志
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <returns>审批日志列表</returns>
    public async Task<List<ApprovalLogDto>> GetLogsAsync(string instanceId)
    {
        try
        {
            var logs = await _logRepository.CreateQuery()
                .Where(x => x.ApprovalInstanceId == long.Parse(instanceId))
                .OrderBy(x => x.OperationTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<ApprovalLogDto>>(logs);

            _logger.LogDebug("获取审批日志成功: 实例ID={InstanceId}, 日志数={Count}",
                instanceId, dtos.Count);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批日志失败: 实例ID={InstanceId}", instanceId);
            return new List<ApprovalLogDto>();
        }
    }

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>操作日志分页列表</returns>
    public async Task<PageList<ApprovalLogDto>> GetUserLogsAsync(string userId, int pageIndex = 1, int pageSize = 20)
    {
        try
        {
            var query = _logRepository.CreateQuery()
                .Where(x => x.OperatorId == userId)
                .OrderByDescending(x => x.OperationTime);

            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ApprovalLogDto>>(logs);

            _logger.LogDebug("获取用户操作日志成功: 用户ID={UserId}, 日志数={Count}",
                userId, dtos.Count);

            return new PageList<ApprovalLogDto>
            {
                Items = dtos,
            Total = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户操作日志失败: 用户ID={UserId}", userId);
            return new PageList<ApprovalLogDto>
            {
                Items = new List<ApprovalLogDto>(),
                Total = 0
            };
        }
    }

    /// <summary>
    /// 获取审批日志统计
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>日志统计</returns>
    public async Task<ApprovalLogStatistics> GetLogStatisticsAsync(DateTime startTime, DateTime endTime, string? userId = null)
    {
        try
        {
            var query = _logRepository.CreateQuery()
                .Where(x => x.OperationTime >= startTime && x.OperationTime <= endTime);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(x => x.OperatorId == userId);
            }

            var statistics = new ApprovalLogStatistics
            {
                StartTime = startTime,
                EndTime = endTime,
                UserId = userId
            };

            // 总操作数
            statistics.TotalOperations = await query.CountAsync();

            // 按操作类型统计
            statistics.OperationsByType = await query
                .GroupBy(x => x.LogType)
                .Select(g => new OperationTypeStatistic
                {
                    LogType = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 按日期统计
            statistics.OperationsByDate = await query
                .GroupBy(x => x.OperationTime.Date)
                .Select(g => new OperationDateStatistic
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // 最活跃用户（如果未指定用户ID）
            if (string.IsNullOrEmpty(userId))
            {
                statistics.TopActiveUsers = await query
                    .GroupBy(x => new { x.OperatorId, x.OperatorName })
                    .Select(g => new UserActivityStatistic
                    {
                        UserId = g.Key.OperatorId,
                        UserName = g.Key.OperatorName,
                        OperationCount = g.Count()
                    })
                    .OrderByDescending(x => x.OperationCount)
                    .Take(10)
                    .ToListAsync();
            }

            _logger.LogDebug("获取审批日志统计成功: 时间范围={StartTime}-{EndTime}, 总操作数={TotalOperations}",
                startTime, endTime, statistics.TotalOperations);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审批日志统计失败: 时间范围={StartTime}-{EndTime}",
                startTime, endTime);

            return new ApprovalLogStatistics
            {
                StartTime = startTime,
                EndTime = endTime,
                UserId = userId,
                TotalOperations = 0,
                OperationsByType = new List<OperationTypeStatistic>(),
                OperationsByDate = new List<OperationDateStatistic>(),
                TopActiveUsers = new List<UserActivityStatistic>()
            };
        }
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的日志数量</returns>
    public async Task<int> CleanupExpiredLogsAsync(int retentionDays = 365)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var expiredLogs = await _logRepository.CreateQuery()
                .Where(x => x.OperationTime < cutoffDate)
                .ToListAsync();

            if (expiredLogs.Any())
            {
                foreach (var log in expiredLogs)
                {
                    await _logRepository.DeleteAsync(log, false);
                }
                await _logRepository.SaveChangesAsync();

                _logger.LogInformation("清理过期审批日志成功: 清理数量={Count}, 保留天数={RetentionDays}",
                    expiredLogs.Count, retentionDays);
            }

            return expiredLogs.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期审批日志失败: 保留天数={RetentionDays}", retentionDays);
            return 0;
        }
    }
}

/// <summary>
/// 审批日志统计
/// </summary>
public class ApprovalLogStatistics
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 用户ID（可选）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 总操作数
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// 按操作类型统计
    /// </summary>
    public List<OperationTypeStatistic> OperationsByType { get; set; } = new();

    /// <summary>
    /// 按日期统计
    /// </summary>
    public List<OperationDateStatistic> OperationsByDate { get; set; } = new();

    /// <summary>
    /// 最活跃用户
    /// </summary>
    public List<UserActivityStatistic> TopActiveUsers { get; set; } = new();
}

/// <summary>
/// 操作类型统计
/// </summary>
public class OperationTypeStatistic
{
    /// <summary>
    /// 日志类型
    /// </summary>
    public ApprovalLogType LogType { get; set; }

    /// <summary>
    /// 操作数量
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// 操作日期统计
/// </summary>
public class OperationDateStatistic
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 操作数量
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// 用户活跃度统计
/// </summary>
public class UserActivityStatistic
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户姓名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 操作数量
    /// </summary>
    public int OperationCount { get; set; }
}
