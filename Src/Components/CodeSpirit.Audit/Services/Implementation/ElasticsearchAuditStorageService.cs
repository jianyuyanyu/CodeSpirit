using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Audit.Helpers;
using Microsoft.Extensions.Logging;
using Elastic.Clients.Elasticsearch;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// Elasticsearch审计存储服务适配器
/// 将现有的IElasticsearchService适配到新的IAuditStorageService接口
/// </summary>
public class ElasticsearchAuditStorageService : IAuditStorageService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<ElasticsearchAuditStorageService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ElasticsearchAuditStorageService(
        IElasticsearchService elasticsearchService,
        ITenantContext? tenantContext = null,
        ILogger<ElasticsearchAuditStorageService>? logger = null)
    {
        _elasticsearchService = elasticsearchService;
        _tenantContext = tenantContext;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ElasticsearchAuditStorageService>.Instance;
    }
    
    /// <summary>
    /// 获取当前租户ID
    /// </summary>
    private string? GetCurrentTenantId()
    {
        return _tenantContext?.TenantId;
    }
    
    /// <summary>
    /// 初始化存储（创建索引）
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            return await _elasticsearchService.CreateIndexAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化Elasticsearch存储失败");
            return false;
        }
    }
    
    /// <summary>
    /// 存储审计日志
    /// </summary>
    public async Task<bool> StoreAsync(Models.AuditLog auditLog)
    {
        try
        {
            return await _elasticsearchService.IndexDocumentAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储审计日志到Elasticsearch失败: {Id}", auditLog.Id);
            return false;
        }
    }
    
    /// <summary>
    /// 批量存储审计日志
    /// </summary>
    public async Task<bool> BulkStoreAsync(IEnumerable<Models.AuditLog> auditLogs)
    {
        try
        {
            return await _elasticsearchService.BulkIndexAsync(auditLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储审计日志到Elasticsearch失败");
            return false;
        }
    }
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    public async Task<Models.AuditLog?> GetByIdAsync(string id)
    {
        try
        {
            return await _elasticsearchService.GetDocumentAsync<Models.AuditLog>(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Elasticsearch获取审计日志失败: {Id}", id);
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
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            if (string.IsNullOrEmpty(query.TenantId))
            {
                query.TenantId = GetCurrentTenantId();
            }
            
            // 构建查询条件列表
            var queryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
            
            // 时间范围查询
            if (query.StartTime.HasValue || query.EndTime.HasValue)
            {
                queryFunctions.Add(AuditQueryHelper.CreateTimeRangeQuery(query.StartTime, query.EndTime));
            }
            
            // 用户ID查询
            if (!string.IsNullOrEmpty(query.UserId))
            {
                queryFunctions.Add(AuditQueryHelper.CreateUserQuery(query.UserId));
            }
            
            // 用户名模糊查询
            if (!string.IsNullOrEmpty(query.UserName))
            {
                queryFunctions.Add(descriptor => descriptor.Query(q => q
                    .Wildcard(w => w
                        .Field(f => f.UserName)
                        .Value($"*{query.UserName}*")
                    )
                ));
            }
            
            // IP地址查询
            if (!string.IsNullOrEmpty(query.IpAddress))
            {
                queryFunctions.Add(AuditQueryHelper.CreateIPQuery(query.IpAddress));
            }
            
            
            // 操作类型查询
            if (query.OperationType.HasValue)
            {
                queryFunctions.Add(AuditQueryHelper.CreateOperationQuery(query.OperationType.Value.ToString()));
            }
            
            
            // 操作成功状态查询
            if (query.IsSuccess.HasValue)
            {
                if (query.IsSuccess.Value)
                {
                    queryFunctions.Add(AuditQueryHelper.CreateSuccessfulOperationsQuery());
                }
                else
                {
                    queryFunctions.Add(AuditQueryHelper.CreateFailedOperationsQuery());
                }
            }
            
            // 租户ID查询
            if (!string.IsNullOrEmpty(query.TenantId))
            {
                queryFunctions.Add(AuditQueryHelper.CreateTenantQuery(query.TenantId));
            }
            
            // 添加分页和排序
            queryFunctions.Add(AuditQueryHelper.CreatePaginationQuery(query.Page, query.PerPage));
            
            var sortField = query.OrderBy ?? "OperationTime";
            var isAscending = query.OrderDir?.ToUpper() == "ASC";
            queryFunctions.Add(AuditQueryHelper.CreateSortQuery(sortField, isAscending));
            
            // 组合所有查询条件
            var combinedFunc = AuditQueryHelper.CombineQueries(queryFunctions.ToArray());
            return await _elasticsearchService.SearchAsync(combinedFunc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch搜索审计日志失败");
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
            return await _elasticsearchService.DeleteDocumentAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Elasticsearch删除审计日志失败: {Id}", id);
            return false;
        }
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = GetCurrentTenantId();
            }
            
            // TODO: 实现操作统计聚合查询，当前返回空字典
            _logger.LogWarning("操作统计功能尚未实现对 ElasticsearchAuditStorageService");
            return new Dictionary<string, long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Elasticsearch操作统计失败");
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
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = GetCurrentTenantId();
            }
            
            // TODO: 实现用户统计聚合查询，当前返回空字典
            _logger.LogWarning("用户统计功能尚未实现对 ElasticsearchAuditStorageService");
            return new Dictionary<string, long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Elasticsearch用户统计失败");
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
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = GetCurrentTenantId();
            }
            
            // TODO: 实现操作趋势聚合查询，当前返回空字典
            _logger.LogWarning("操作趋势功能尚未实现对 ElasticsearchAuditStorageService");
            return new Dictionary<DateTime, long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Elasticsearch操作趋势失败");
            return new Dictionary<DateTime, long>();
        }
    }
    
    /// <summary>
    /// 健康检查
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            return await _elasticsearchService.IndexExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch健康检查失败");
            return false;
        }
    }
}
