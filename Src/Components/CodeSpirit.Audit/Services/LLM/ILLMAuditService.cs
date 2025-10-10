using CodeSpirit.Audit.Services.LLM.Dtos;

namespace CodeSpirit.Audit.Services.LLM;

/// <summary>
/// LLM审计服务接口
/// </summary>
public interface ILLMAuditService
{
    /// <summary>
    /// 记录LLM交互审计
    /// </summary>
    /// <param name="auditLog">审计日志</param>
    Task LogLLMInteractionAsync(Models.LLM.LLMAuditLog auditLog);
    
    /// <summary>
    /// 批量记录LLM审计
    /// </summary>
    /// <param name="auditLogs">审计日志集合</param>
    Task LogBatchLLMInteractionsAsync(IEnumerable<Models.LLM.LLMAuditLog> auditLogs);
    
    /// <summary>
    /// 查询LLM审计日志
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>审计日志列表和总数</returns>
    Task<(IEnumerable<Models.LLM.LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query);
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>审计日志</returns>
    Task<Models.LLM.LLMAuditLog?> GetByIdAsync(string id);
    
    /// <summary>
    /// 获取LLM使用统计
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>使用统计</returns>
    Task<LLMUsageStatsDto> GetUsageStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null);
    
    /// <summary>
    /// 获取LLM成本统计
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>成本统计</returns>
    Task<LLMCostStatsDto> GetCostStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null);
    
    /// <summary>
    /// 获取LLM质量统计
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>质量统计</returns>
    Task<LLMQualityStatsDto> GetQualityStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null);
    
    /// <summary>
    /// 获取LLM使用趋势
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="intervalHours">时间间隔（小时）</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>使用趋势</returns>
    Task<Dictionary<DateTime, long>> GetUsageTrendAsync(DateTime startTime, DateTime endTime, int intervalHours = 24, string? tenantId = null);
    
    /// <summary>
    /// 健康检查
    /// </summary>
    /// <returns>是否健康</returns>
    Task<bool> HealthCheckAsync();
}

