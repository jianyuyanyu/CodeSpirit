using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Audit.Helpers;
using Microsoft.Extensions.Logging;
using Elastic.Clients.Elasticsearch;
using System.Collections.Generic;

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
            
            var queryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
            
            // 时间范围查询
            queryFunctions.Add(AuditQueryHelper.CreateTimeRangeQuery(startTime, endTime));
            
            // 租户过滤
            if (!string.IsNullOrEmpty(tenantId))
            {
                queryFunctions.Add(AuditQueryHelper.CreateTenantQuery(tenantId));
            }
            
            // 组合查询条件
            var combinedQuery = AuditQueryHelper.CombineQueries(queryFunctions.ToArray());
            
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
            {
                var descriptor = combinedQuery(s);
                return descriptor
                    .Aggregations(a => a
                        .Add("operation_stats", agg => agg
                            .Terms(t => t
                                .Field(f => f.OperationType)
                                .Size(100)
                            )
                        )
                    );
            });
            
            var stats = new Dictionary<string, long>();
            
            if (result != null && result.TryGetValue("operation_stats", out var operationStatsObj))
            {
                if (operationStatsObj is Dictionary<string, object> operationStatsDict)
                {
                    if (operationStatsDict.TryGetValue("buckets", out var bucketsObj) && bucketsObj is System.Collections.IList buckets)
                    {
                        foreach (var bucket in buckets)
                        {
                            if (bucket is Dictionary<string, object> bucketDict)
                            {
                                var key = bucketDict.TryGetValue("key", out var keyObj) ? keyObj?.ToString() ?? "" : "";
                                var docCount = bucketDict.TryGetValue("doc_count", out var countObj) 
                                    ? Convert.ToInt64(countObj) 
                                    : 0L;
                                
                                if (!string.IsNullOrEmpty(key))
                                {
                                    stats[key] = docCount;
                                }
                            }
                        }
                    }
                }
            }
            
            return stats;
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
            
            var queryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
            
            // 时间范围查询
            queryFunctions.Add(AuditQueryHelper.CreateTimeRangeQuery(startTime, endTime));
            
            // 用户名为非空
            queryFunctions.Add(s => s.Query(q => q.Exists(e => e.Field(f => f.UserName))));
            
            // 租户过滤
            if (!string.IsNullOrEmpty(tenantId))
            {
                queryFunctions.Add(AuditQueryHelper.CreateTenantQuery(tenantId));
            }
            
            // 组合查询条件
            var combinedQuery = AuditQueryHelper.CombineQueries(queryFunctions.ToArray());
            
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
            {
                var descriptor = combinedQuery(s);
                return descriptor
                    .Aggregations(a => a
                        .Add("user_stats", agg => agg
                            .Terms(t => t
                                .Field(f => f.UserName)
                                .Size(topN)
                            )
                        )
                    );
            });
            
            var stats = new Dictionary<string, long>();
            
            if (result != null && result.TryGetValue("user_stats", out var userStatsObj))
            {
                if (userStatsObj is Dictionary<string, object> userStatsDict)
                {
                    if (userStatsDict.TryGetValue("buckets", out var bucketsObj) && bucketsObj is System.Collections.IList buckets)
                    {
                        foreach (var bucket in buckets)
                        {
                            if (bucket is Dictionary<string, object> bucketDict)
                            {
                                var key = bucketDict.TryGetValue("key", out var keyObj) ? keyObj?.ToString() ?? "" : "";
                                var docCount = bucketDict.TryGetValue("doc_count", out var countObj) 
                                    ? Convert.ToInt64(countObj) 
                                    : 0L;
                                
                                if (!string.IsNullOrEmpty(key))
                                {
                                    stats[key] = docCount;
                                }
                            }
                        }
                    }
                }
            }
            
            return stats;
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
            
            // 确定时间间隔格式
            string intervalFormat = interval switch
            {
                1 => "1h",      // 1小时
                6 => "6h",      // 6小时
                12 => "12h",    // 12小时
                24 => "1d",     // 1天
                168 => "1w",    // 1周
                _ => $"{interval}h"  // 自定义小时数
            };
            
            var queryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
            
            // 时间范围查询
            queryFunctions.Add(AuditQueryHelper.CreateTimeRangeQuery(startTime, endTime));
            
            // 租户过滤
            if (!string.IsNullOrEmpty(tenantId))
            {
                queryFunctions.Add(AuditQueryHelper.CreateTenantQuery(tenantId));
            }
            
            // 组合查询条件
            var combinedQuery = AuditQueryHelper.CombineQueries(queryFunctions.ToArray());
            
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
            {
                var descriptor = combinedQuery(s);
                return descriptor
                    .Aggregations(a => a
                        .Add("operation_trend", agg => agg
                            .DateHistogram(dh => dh
                                .Field(f => f.OperationTime)
                                .FixedInterval(intervalFormat)
                            )
                        )
                    );
            });
            
            var trend = new Dictionary<DateTime, long>();
            
            if (result != null && result.TryGetValue("operation_trend", out var trendObj))
            {
                if (trendObj is Dictionary<string, object> trendDict)
                {
                    if (trendDict.TryGetValue("buckets", out var bucketsObj) && bucketsObj is System.Collections.IList buckets)
                    {
                        foreach (var bucket in buckets)
                        {
                            if (bucket is Dictionary<string, object> bucketDict)
                            {
                                var keyAsString = bucketDict.TryGetValue("key_as_string", out var keyStrObj) 
                                    ? keyStrObj?.ToString() 
                                    : null;
                                
                                var key = bucketDict.TryGetValue("key", out var keyObj) 
                                    ? keyObj 
                                    : null;
                                
                                var docCount = bucketDict.TryGetValue("doc_count", out var countObj) 
                                    ? Convert.ToInt64(countObj) 
                                    : 0L;
                                
                                DateTime dateTime;
                                if (!string.IsNullOrEmpty(keyAsString) && DateTime.TryParse(keyAsString, out dateTime))
                                {
                                    trend[dateTime] = docCount;
                                }
                                else if (key != null)
                                {
                                    // 尝试从时间戳转换
                                    if (key is long timestamp)
                                    {
                                        dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                                        trend[dateTime] = docCount;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Elasticsearch操作趋势失败");
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
            // 计算时间范围
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var last7DaysStart = todayStart.AddDays(-7);
            
            // 如果未指定租户ID且当前上下文有租户，则自动应用租户过滤
            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = GetCurrentTenantId();
            }
            
            // 构建查询条件
            var queryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
            
            // 今日时间范围查询
            var todayQueryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>
            {
                AuditQueryHelper.CreateTimeRangeQuery(todayStart, now)
            };
            
            // 租户过滤
            if (!string.IsNullOrEmpty(tenantId))
            {
                todayQueryFunctions.Add(AuditQueryHelper.CreateTenantQuery(tenantId));
            }
            
            var todayQuery = AuditQueryHelper.CombineQueries(todayQueryFunctions.ToArray());
            
            // 最近7天查询
            var last7DaysQueryFunctions = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>
            {
                AuditQueryHelper.CreateTimeRangeQuery(last7DaysStart, now)
            };
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                last7DaysQueryFunctions.Add(AuditQueryHelper.CreateTenantQuery(tenantId));
            }
            
            var last7DaysQuery = AuditQueryHelper.CombineQueries(last7DaysQueryFunctions.ToArray());
            
            // 并行执行查询 - 使用 SearchAsync 获取总数（Size=0 只返回总数，不返回文档）
            var todayTotalTask = _elasticsearchService.SearchAsync<Models.AuditLog>(s => todayQuery(s).Size(0));
            var todaySuccessTask = _elasticsearchService.SearchAsync<Models.AuditLog>(s => todayQuery(s).Query(q => q.Bool(b => b.Must(m => m.Term(t => t.Field(f => f.IsSuccess).Value(true))))).Size(0));
            var todayFailedTask = _elasticsearchService.SearchAsync<Models.AuditLog>(s => todayQuery(s).Query(q => q.Bool(b => b.Must(m => m.Term(t => t.Field(f => f.IsSuccess).Value(false))))).Size(0));
            var last7DaysTotalTask = _elasticsearchService.SearchAsync<Models.AuditLog>(s => last7DaysQuery(s).Size(0));
            
            // 平均响应时长查询（使用聚合）
            var avgResponseTimeTask = _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
            {
                var descriptor = todayQuery(s);
                return descriptor
                    .Query(q => q.Bool(b => b.Must(m => m.Exists(e => e.Field(f => f.ExecutionDuration)))))
                    .Aggregations(a => a
                        .Add("avg_duration", agg => agg.Avg(av => av.Field(f => f.ExecutionDuration)))
                    );
            });
            
            // 系统审计专用查询（仅当未指定租户时执行）
            Task<long>? todayActiveTenantsTask = null;
            Task<long>? todayActiveUsersTask = null;
            
            if (string.IsNullOrEmpty(tenantId))
            {
                // 活跃租户数（去重统计）
                todayActiveTenantsTask = Task.Run(async () =>
                {
                    var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
                    {
                        var descriptor = todayQuery(s);
                        return descriptor
                            .Query(q => q.Bool(b => b.Must(m => m.Exists(e => e.Field(f => f.TenantId)))))
                            .Aggregations(a => a
                                .Add("unique_tenants", agg => agg.Cardinality(c => c.Field(f => f.TenantId)))
                            );
                    });
                    
                    if (result != null && result.TryGetValue("unique_tenants", out var tenantsObj))
                    {
                        if (tenantsObj is Dictionary<string, object> tenantsDict && tenantsDict.TryGetValue("value", out var valueObj))
                        {
                            return Convert.ToInt64(valueObj);
                        }
                    }
                    return 0L;
                });
                
                // 活跃用户数（去重统计）
                todayActiveUsersTask = Task.Run(async () =>
                {
                    var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(s =>
                    {
                        var descriptor = todayQuery(s);
                        return descriptor
                            .Query(q => q.Bool(b => b.Must(m => m.Exists(e => e.Field(f => f.UserId)))))
                            .Aggregations(a => a
                                .Add("unique_users", agg => agg.Cardinality(c => c.Field(f => f.UserId)))
                            );
                    });
                    
                    if (result != null && result.TryGetValue("unique_users", out var usersObj))
                    {
                        if (usersObj is Dictionary<string, object> usersDict && usersDict.TryGetValue("value", out var valueObj))
                        {
                            return Convert.ToInt64(valueObj);
                        }
                    }
                    return 0L;
                });
            }
            
            // 等待所有查询完成
            await Task.WhenAll(
                todayTotalTask,
                todaySuccessTask,
                todayFailedTask,
                last7DaysTotalTask,
                avgResponseTimeTask,
                todayActiveTenantsTask ?? Task.FromResult(0L),
                todayActiveUsersTask ?? Task.FromResult(0L)
            );
            
            // 解析结果
            var todayTotalResult = await todayTotalTask;
            var todaySuccessResult = await todaySuccessTask;
            var todayFailedResult = await todayFailedTask;
            var last7DaysTotalResult = await last7DaysTotalTask;
            
            var todayTotal = todayTotalResult.Total;
            var todaySuccess = todaySuccessResult.Total;
            var todayFailed = todayFailedResult.Total;
            var last7DaysTotal = last7DaysTotalResult.Total;
            
            var avgResponseTime = 0.0;
            if (avgResponseTimeTask.Result != null && avgResponseTimeTask.Result.TryGetValue("avg_duration", out var avgObj))
            {
                if (avgObj is Dictionary<string, object> avgDict && avgDict.TryGetValue("value", out var valueObj))
                {
                    avgResponseTime = Convert.ToDouble(valueObj ?? 0.0);
                }
            }
            
            var todayActiveTenants = todayActiveTenantsTask != null ? await todayActiveTenantsTask : 0L;
            var todayActiveUsers = todayActiveUsersTask != null ? await todayActiveUsersTask : 0L;
            
            // 计算成功率
            var successRate = todayTotal > 0 ? (todaySuccess * 100.0 / todayTotal) : 0.0;
            
            var result = new AuditCardsStatsDto
            {
                TodayTotal = todayTotal,
                TodaySuccess = todaySuccess,
                TodayFailed = todayFailed,
                SuccessRate = successRate,
                TodayActiveTenants = todayActiveTenants,
                TodayActiveUsers = todayActiveUsers,
                Last7DaysTotal = last7DaysTotal,
                AvgResponseTime = avgResponseTime
            };
            
            _logger.LogDebug("获取Elasticsearch审计卡片统计成功，租户: {TenantId}, 今日总数: {Total}, 成功率: {SuccessRate:F2}%",
                tenantId ?? "全部", todayTotal, successRate);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Elasticsearch审计卡片统计失败");
            return new AuditCardsStatsDto();
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
