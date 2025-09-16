using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Services;

/// <summary>
/// 审批日志服务接口
/// </summary>
public interface IApprovalLogService : IScopedDependency
{
    /// <summary>
    /// 记录审批日志
    /// </summary>
    /// <param name="log">审批日志</param>
    /// <returns>记录结果</returns>
    Task<bool> LogAsync(ApprovalLog log);

    /// <summary>
    /// 批量记录审批日志
    /// </summary>
    /// <param name="logs">审批日志列表</param>
    /// <returns>记录结果</returns>
    Task<bool> LogBatchAsync(List<ApprovalLog> logs);

    /// <summary>
    /// 获取审批日志
    /// </summary>
    /// <param name="instanceId">审批实例ID</param>
    /// <returns>审批日志列表</returns>
    Task<List<ApprovalLogDto>> GetLogsAsync(string instanceId);

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>操作日志分页列表</returns>
    Task<PageList<ApprovalLogDto>> GetUserLogsAsync(string userId, int pageIndex = 1, int pageSize = 20);

    /// <summary>
    /// 获取审批日志统计
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <returns>日志统计</returns>
    Task<ApprovalLogStatistics> GetLogStatisticsAsync(DateTime startTime, DateTime endTime, string? userId = null);

    /// <summary>
    /// 清理过期日志
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的日志数量</returns>
    Task<int> CleanupExpiredLogsAsync(int retentionDays = 365);
}
