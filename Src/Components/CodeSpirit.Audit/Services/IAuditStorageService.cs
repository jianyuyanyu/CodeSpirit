using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计存储服务接口
/// 提供统一的审计日志存储抽象，支持多种存储后端
/// </summary>
public interface IAuditStorageService
{
    /// <summary>
    /// 初始化存储（创建索引/表等）
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> InitializeAsync();
    
    /// <summary>
    /// 存储审计日志
    /// </summary>
    /// <param name="auditLog">审计日志</param>
    /// <returns>是否成功</returns>
    Task<bool> StoreAsync(Models.AuditLog auditLog);
    
    /// <summary>
    /// 批量存储审计日志
    /// </summary>
    /// <param name="auditLogs">审计日志集合</param>
    /// <returns>是否成功</returns>
    Task<bool> BulkStoreAsync(IEnumerable<Models.AuditLog> auditLogs);
    
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
    /// <returns>审计日志列表和总数</returns>
    Task<(IEnumerable<Models.AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query);
    
    /// <summary>
    /// 删除审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(string id);
    
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
    
    /// <summary>
    /// 健康检查
    /// </summary>
    /// <returns>是否健康</returns>
    Task<bool> HealthCheckAsync();
}
