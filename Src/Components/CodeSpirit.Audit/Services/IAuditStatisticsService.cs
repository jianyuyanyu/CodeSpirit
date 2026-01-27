using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计统计服务接口
/// </summary>
/// <remarks>
/// 专门负责审计日志的统计功能，职责单一
/// </remarks>
public interface IAuditStatisticsService
{
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
    
    /// <summary>
    /// 获取审计卡片统计数据
    /// </summary>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>统计数据</returns>
    Task<AuditCardsStatsDto> GetCardsStatsAsync(string? tenantId = null);
}
