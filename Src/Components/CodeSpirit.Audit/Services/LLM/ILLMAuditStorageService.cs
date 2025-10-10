using CodeSpirit.Audit.Services.LLM.Dtos;

namespace CodeSpirit.Audit.Services.LLM;

/// <summary>
/// LLM审计存储服务接口
/// </summary>
public interface ILLMAuditStorageService
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
    Task<bool> StoreAsync(Models.LLM.LLMAuditLog auditLog);
    
    /// <summary>
    /// 批量存储审计日志
    /// </summary>
    /// <param name="auditLogs">审计日志集合</param>
    /// <returns>是否成功</returns>
    Task<bool> BulkStoreAsync(IEnumerable<Models.LLM.LLMAuditLog> auditLogs);
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>审计日志</returns>
    Task<Models.LLM.LLMAuditLog?> GetByIdAsync(string id);
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>审计日志列表和总数</returns>
    Task<(IEnumerable<Models.LLM.LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query);
    
    /// <summary>
    /// 获取聚合统计
    /// </summary>
    /// <param name="field">聚合字段</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>聚合结果</returns>
    Task<Dictionary<string, long>> GetAggregationAsync(string field, DateTime startTime, DateTime endTime, string? tenantId = null);
    
    /// <summary>
    /// 健康检查
    /// </summary>
    /// <returns>是否健康</returns>
    Task<bool> HealthCheckAsync();
}

