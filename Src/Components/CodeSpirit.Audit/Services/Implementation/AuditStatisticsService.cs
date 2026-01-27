using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计统计服务实现
/// </summary>
/// <remarks>
/// 专门负责审计日志的统计功能
/// </remarks>
public class AuditStatisticsService : IAuditStatisticsService
{
    private readonly IAuditStorageService _storageService;
    private readonly ILogger<AuditStatisticsService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditStatisticsService(
        IAuditStorageService storageService,
        ILogger<AuditStatisticsService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            return await _storageService.GetOperationStatsAsync(startTime, endTime, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作统计失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null)
    {
        try
        {
            return await _storageService.GetUserStatsAsync(startTime, endTime, topN, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    public async Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null)
    {
        try
        {
            return await _storageService.GetOperationTrendAsync(startTime, endTime, interval, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作趋势失败");
            return new Dictionary<DateTime, long>();
        }
    }
    
    /// <summary>
    /// 获取审计卡片统计数据
    /// </summary>
    public async Task<AuditCardsStatsDto> GetCardsStatsAsync(string? tenantId = null)
    {
        try
        {
            return await _storageService.GetCardsStatsAsync(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计卡片统计失败");
            return new AuditCardsStatsDto();
        }
    }
}
