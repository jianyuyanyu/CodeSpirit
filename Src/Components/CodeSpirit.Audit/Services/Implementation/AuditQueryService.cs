using CodeSpirit.Audit.Helpers;
using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计查询服务实现
/// </summary>
/// <remarks>
/// 专门负责审计日志的查询功能
/// </remarks>
public class AuditQueryService : IAuditQueryService
{
    private readonly IAuditStorageService _storageService;
    private readonly ILogger<AuditQueryService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditQueryService(
        IAuditStorageService storageService,
        ILogger<AuditQueryService> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    public async Task<Models.AuditLog?> GetByIdAsync(string id)
    {
        try
        {
            return await _storageService.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取审计日志失败: {Id}", id);
            return null;
        }
    }
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    public async Task<(IEnumerable<Models.AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query)
    {
        try
        {
            _logger.LogInformation("开始搜索审计日志 - 页码: {Page}, 页大小: {PerPage}, 排序: {OrderBy} {OrderDir}", 
                query.Page, query.PerPage, query.OrderBy ?? "OperationTime", query.OrderDir);
            
            var result = await _storageService.SearchAsync(query);
            
            _logger.LogInformation("审计日志搜索完成 - 返回 {Count} 条记录，总计 {Total} 条", 
                result.Items.Count(), result.Total);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索审计日志失败");
            return (Enumerable.Empty<Models.AuditLog>(), 0);
        }
    }
    
    /// <summary>
    /// 删除审计日志
    /// </summary>
    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            return await _storageService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除审计日志失败: {Id}", id);
            return false;
        }
    }
}
