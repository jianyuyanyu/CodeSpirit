using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计服务实现（向后兼容）
/// </summary>
/// <remarks>
/// 此服务已重构为三个专门的服务：IAuditRecorder、IAuditQueryService、IAuditStatisticsService
/// 为了保持向后兼容，此服务内部委托给新的服务
/// </remarks>
public class AuditService : IAuditService
{
    private readonly IAuditRecorder _recorder;
    private readonly IAuditQueryService _queryService;
    private readonly IAuditStatisticsService _statisticsService;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditService(
        IAuditRecorder recorder,
        IAuditQueryService queryService,
        IAuditStatisticsService statisticsService)
    {
        _recorder = recorder;
        _queryService = queryService;
        _statisticsService = statisticsService;
    }
    
    /// <summary>
    /// 记录审计日志
    /// </summary>
    public async Task LogAsync(Models.AuditLog auditLog)
    {
        await _recorder.RecordAsync(auditLog);
    }
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    public async Task<Models.AuditLog?> GetByIdAsync(string id)
    {
        return await _queryService.GetByIdAsync(id);
    }
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    public async Task<(IEnumerable<Models.AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query)
    {
        return await _queryService.SearchAsync(query);
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        return await _statisticsService.GetOperationStatsAsync(startTime, endTime, tenantId);
    }
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null)
    {
        return await _statisticsService.GetUserStatsAsync(startTime, endTime, topN, tenantId);
    }
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    public async Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null)
    {
        return await _statisticsService.GetOperationTrendAsync(startTime, endTime, interval, tenantId);
    }
    
    /// <summary>
    /// 获取审计卡片统计数据
    /// </summary>
    public async Task<AuditCardsStatsDto> GetCardsStatsAsync(string? tenantId = null)
    {
        return await _statisticsService.GetCardsStatsAsync(tenantId);
    }
} 