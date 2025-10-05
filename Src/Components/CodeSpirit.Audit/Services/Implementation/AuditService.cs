using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Audit.Helpers;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计服务实现
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditStorageService _storageService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditOptions _options;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditService(
        IAuditStorageService storageService,
        IRabbitMQService rabbitMQService,
        ILogger<AuditService> logger,
        IConfiguration configuration)
    {
        _storageService = storageService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
        
        // 使用配置助手简化配置绑定
        _options = ConfigurationHelper.BindAuditOptions(configuration);
    }
    
    /// <summary>
    /// 记录审计日志
    /// </summary>
    public async Task LogAsync(Models.AuditLog auditLog)
    {
        try
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("审计功能已禁用，跳过记录");
                return;
            }
            
            _logger.LogDebug("开始记录审计日志: {Id}", auditLog.Id);
            
            try 
            {
                // 将审计日志推送到RabbitMQ
                await _rabbitMQService.SendMessageAsync(auditLog);
                _logger.LogDebug("审计日志已推送到消息队列: {Id}", auditLog.Id);
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                // RabbitMQ不可用时直接写入Elasticsearch
                _logger.LogWarning(ex, "RabbitMQ服务不可用，正在直接写入Elasticsearch");
                await _storageService.StoreAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "推送审计日志到RabbitMQ失败，尝试直接写入Elasticsearch");
                // 如果RabbitMQ出错，尝试直接写入Elasticsearch
                await _storageService.StoreAsync(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录审计日志失败: {Id}", auditLog.Id);
        }
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
            _logger.LogError(ex, "根据ID获取审计日志失败");
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
            // 记录查询参数
            _logger.LogInformation("开始搜索审计日志");
            _logger.LogInformation("查询参数 - 页码: {PageIndex}, 页大小: {PageSize}", query.Page, query.PerPage);
            _logger.LogInformation("排序字段: {SortField}, 排序方向: {SortDirection}", query.OrderBy ?? "OperationTime", query.OrderDir);
            
            if (!string.IsNullOrEmpty(query.UserId))
                _logger.LogInformation("用户ID过滤: {UserId}", query.UserId);
            if (query.OperationType.HasValue)
                _logger.LogInformation("操作类型过滤: {OperationType}", query.OperationType.Value);
            if (query.StartTime.HasValue || query.EndTime.HasValue)
                _logger.LogInformation("时间范围: {StartTime} - {EndTime}", query.StartTime, query.EndTime);
            if (!string.IsNullOrEmpty(query.Keywords))
                _logger.LogInformation("关键词搜索: {Keyword}", query.Keywords);
            
            // 构建复合查询
            var searchFunc = BuildSearchQuery(query);
            
            // 确定排序字段和方向
            var sortField = string.IsNullOrEmpty(query.OrderBy) ? "OperationTime" : query.OrderBy;
            var isAscending = query.OrderDir?.ToLower() == "asc";
            
            _logger.LogInformation("最终排序配置 - 字段: {SortField}, 升序: {IsAscending}", sortField, isAscending);
            
            // 添加分页和排序
            var combinedFunc = AuditQueryHelper.CombineQueries(
                searchFunc,
                AuditQueryHelper.CreatePaginationQuery(query.Page, query.PerPage),
                AuditQueryHelper.CreateSortQuery(sortField, isAscending)
            );
            
            _logger.LogInformation("开始执行Elasticsearch查询...");
            var result = await _storageService.SearchAsync(query);
            
            _logger.LogInformation("审计日志搜索完成 - 返回 {Count} 条记录，总计 {Total} 条", 
                result.Items.Count(), result.Total);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索审计日志失败");
            _logger.LogError("查询参数: {Query}", System.Text.Json.JsonSerializer.Serialize(query));
            return (Enumerable.Empty<Models.AuditLog>(), 0);
        }
    }
    
    /// <summary>
    /// 构建搜索查询
    /// </summary>
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> BuildSearchQuery(AuditLogQueryDto query)
    {
        var queries = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
        
        // 租户ID查询
        if (!string.IsNullOrEmpty(query.TenantId))
        {
            queries.Add(AuditQueryHelper.CreateTenantQuery(query.TenantId));
        }
        
        // 用户ID查询
        if (!string.IsNullOrEmpty(query.UserId))
        {
            queries.Add(AuditQueryHelper.CreateUserQuery(query.UserId));
        }
        
        // 操作类型查询
        if (query.OperationType.HasValue)
        {
            queries.Add(AuditQueryHelper.CreateOperationQuery(query.OperationType.Value.ToString()));
        }
        
        // 时间范围查询
        if (query.StartTime.HasValue || query.EndTime.HasValue)
        {
            queries.Add(AuditQueryHelper.CreateTimeRangeQuery(query.StartTime, query.EndTime));
        }
        
        // 关键词搜索
        if (!string.IsNullOrEmpty(query.Keywords))
        {
            queries.Add(AuditQueryHelper.CreateTextQuery(query.Keywords));
        }
        
        // 如果没有任何查询条件，返回匹配所有的查询
        if (queries.Count == 0)
        {
            return s => s.From((query.Page - 1) * query.PerPage)
                         .Size(query.PerPage);
        }
        
        // 组合所有查询条件
        return AuditQueryHelper.CombineQueries(queries.ToArray());
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>统计信息</returns>
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
    /// 创建操作统计聚合查询
    /// </summary>
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateOperationStatsAggregation(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        return s =>
        {
            var searchRequest = s.Size(0); // 不返回具体文档，只返回聚合结果
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                searchRequest = searchRequest.Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .Range(r => r
                                .DateRange(dr => dr
                                    .Field(f => f.OperationTime)
                                    .Gte(startTime)
                                    .Lte(endTime)
                                )
                            )
                        )
                        .Must(m => m
                            .Term(t => t
                                .Field(f => f.TenantId)
                                .Value(tenantId)
                            )
                        )
                    )
                );
            }
            else
            {
                searchRequest = searchRequest.Query(q => q
                    .Range(r => r
                        .DateRange(dr => dr
                            .Field(f => f.OperationTime)
                            .Gte(startTime)
                            .Lte(endTime)
                        )
                    )
                );
            }
            
            return searchRequest.Aggregations(a => a
                .Add("operations", agg => agg
                    .Terms(t => t
                        .Field(f => f.OperationType)
                        .Size(50)
                    )
                )
            );
        };
    }
    
    /// <summary>
    /// 解析操作统计结果
    /// </summary>
    private Dictionary<string, long> ParseOperationStatsResult(IDictionary<string, object> result)
    {
        var stats = new Dictionary<string, long>();
        
        try
        {
            if (result.TryGetValue("operations", out var operationsObj))
            {
                _logger.LogDebug("操作统计结果: {Result}", System.Text.Json.JsonSerializer.Serialize(operationsObj));
                
                // 解析Terms聚合结果
                if (operationsObj is Dictionary<string, object> operationsDict)
                {
                    if (operationsDict.TryGetValue("buckets", out var bucketsObj))
                    {
                        if (bucketsObj is List<Dictionary<string, object>> buckets)
                        {
                            foreach (var bucket in buckets)
                            {
                                if (bucket.TryGetValue("key", out var keyObj) && 
                                    bucket.TryGetValue("doc_count", out var countObj))
                                {
                                    var key = keyObj?.ToString() ?? "未知";
                                    var count = Convert.ToInt64(countObj);
                                    stats[key] = count;
                                    
                                    _logger.LogDebug("操作统计 - {Operation}: {Count}", key, count);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析操作统计结果失败");
        }
        
        return stats;
    }
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="topN">前N个用户</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>用户统计信息</returns>
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
    /// 创建用户统计聚合查询
    /// </summary>
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateUserStatsAggregation(DateTime startTime, DateTime endTime, int topN, string? tenantId = null)
    {
        return s =>
        {
            var searchRequest = s.Size(0);
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                searchRequest = searchRequest.Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .Range(r => r
                                .DateRange(dr => dr
                                    .Field(f => f.OperationTime)
                                    .Gte(startTime)
                                    .Lte(endTime)
                                )
                            )
                        )
                        .Must(m => m
                            .Term(t => t
                                .Field(f => f.TenantId)
                                .Value(tenantId)
                            )
                        )
                    )
                );
            }
            else
            {
                searchRequest = searchRequest.Query(q => q
                    .Range(r => r
                        .DateRange(dr => dr
                            .Field(f => f.OperationTime)
                            .Gte(startTime)
                            .Lte(endTime)
                        )
                    )
                );
            }
            
            return searchRequest.Aggregations(a => a
                .Add("users", agg => agg
                    .Terms(t => t
                        .Field(f => f.UserId)
                        .Size(topN)
                    )
                )
            );
        };
    }
    
    /// <summary>
    /// 解析用户统计结果
    /// </summary>
    private Dictionary<string, long> ParseUserStatsResult(IDictionary<string, object> result)
    {
        var stats = new Dictionary<string, long>();
        
        try
        {
            if (result.TryGetValue("users", out var usersObj))
            {
                _logger.LogDebug("用户统计结果: {Result}", System.Text.Json.JsonSerializer.Serialize(usersObj));
                
                // 解析Terms聚合结果
                if (usersObj is Dictionary<string, object> usersDict)
                {
                    if (usersDict.TryGetValue("buckets", out var bucketsObj))
                    {
                        if (bucketsObj is List<Dictionary<string, object>> buckets)
                        {
                            foreach (var bucket in buckets)
                            {
                                if (bucket.TryGetValue("key", out var keyObj) && 
                                    bucket.TryGetValue("doc_count", out var countObj))
                                {
                                    var key = keyObj?.ToString() ?? "未知用户";
                                    var count = Convert.ToInt64(countObj);
                                    stats[key] = count;
                                    
                                    _logger.LogDebug("用户统计 - {UserId}: {Count}", key, count);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析用户统计结果失败");
        }
        
        return stats;
    }
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="interval">时间间隔(小时)</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>操作趋势</returns>
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
    /// 创建操作趋势聚合查询
    /// </summary>
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateOperationTrendAggregation(DateTime startTime, DateTime endTime, int interval, string? tenantId = null)
    {
        return s =>
        {
            var searchRequest = s.Size(0);
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                searchRequest = searchRequest.Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .Range(r => r
                                .DateRange(dr => dr
                                    .Field(f => f.OperationTime)
                                    .Gte(startTime)
                                    .Lte(endTime)
                                )
                            )
                        )
                        .Must(m => m
                            .Term(t => t
                                .Field(f => f.TenantId)
                                .Value(tenantId)
                            )
                        )
                    )
                );
            }
            else
            {
                searchRequest = searchRequest.Query(q => q
                    .Range(r => r
                        .DateRange(dr => dr
                            .Field(f => f.OperationTime)
                            .Gte(startTime)
                            .Lte(endTime)
                        )
                    )
                );
            }
            
            return searchRequest.Aggregations(a => a
                .Add("trend", agg => agg
                    .DateHistogram(dh => dh
                        .Field(f => f.OperationTime)
                        .FixedInterval($"{interval}h")
                        .MinDocCount(0)
                    )
                )
            );
        };
    }
    
    /// <summary>
    /// 解析操作趋势结果
    /// </summary>
    private Dictionary<DateTime, long> ParseOperationTrendResult(IDictionary<string, object> result)
    {
        var trend = new Dictionary<DateTime, long>();
        
        try
        {
            if (result.TryGetValue("trend", out var trendObj))
            {
                _logger.LogDebug("操作趋势结果: {Result}", System.Text.Json.JsonSerializer.Serialize(trendObj));
                
                // 解析DateHistogram聚合结果
                if (trendObj is Dictionary<string, object> trendDict)
                {
                    if (trendDict.TryGetValue("buckets", out var bucketsObj))
                    {
                        if (bucketsObj is List<Dictionary<string, object>> buckets)
                        {
                            foreach (var bucket in buckets)
                            {
                                if (bucket.TryGetValue("key", out var keyObj) && 
                                    bucket.TryGetValue("doc_count", out var countObj))
                                {
                                    // 尝试解析时间戳
                                    DateTime dateTime;
                                    if (keyObj is long timestamp)
                                    {
                                        // 处理毫秒级Unix时间戳
                                        try
                                        {
                                            dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                                        }
                                        catch (ArgumentOutOfRangeException ex)
                                        {
                                            _logger.LogWarning("时间戳超出有效范围: {Timestamp}, 错误: {Error}", timestamp, ex.Message);
                                            continue;
                                        }
                                    }
                                    else if (keyObj is double doubleTimestamp)
                                    {
                                        // 处理浮点数时间戳
                                        try
                                        {
                                            var longTimestamp = Convert.ToInt64(doubleTimestamp);
                                            dateTime = DateTimeOffset.FromUnixTimeMilliseconds(longTimestamp).DateTime;
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning("无法转换浮点数时间戳: {Timestamp}, 错误: {Error}", doubleTimestamp, ex.Message);
                                            continue;
                                        }
                                    }
                                    else if (DateTime.TryParse(keyObj?.ToString(), out var parsedDate))
                                    {
                                        dateTime = parsedDate;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("无法解析时间戳: {Key}", keyObj);
                                        continue;
                                    }
                                    
                                    var count = Convert.ToInt64(countObj);
                                    trend[dateTime] = count;
                                    
                                    _logger.LogDebug("操作趋势 - {DateTime}: {Count}", dateTime, count);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析操作趋势结果失败");
        }
        
        return trend;
    }
} 