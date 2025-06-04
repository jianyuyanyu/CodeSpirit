using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计服务接口
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="auditLog">审计日志</param>
    /// <returns>任务</returns>
    Task LogAsync(Models.AuditLog auditLog);
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>审计日志</returns>
    Task<Models.AuditLog?> GetByIdAsync(string id);
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>审计日志列表</returns>
    Task<(IEnumerable<Models.AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query);
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>统计信息</returns>
    Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null);
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="topN">前N个用户</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>用户统计信息</returns>
    Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null);
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="interval">时间间隔(小时)</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>操作趋势</returns>
    Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null);
}
