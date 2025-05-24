using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计服务实现
/// </summary>
public class AuditService : IAuditService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditOptions _options;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditService(
        IElasticsearchService elasticsearchService,
        IRabbitMQService rabbitMQService,
        ILogger<AuditService> logger,
        IConfiguration configuration)
    {
        _elasticsearchService = elasticsearchService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options;
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
                return;
            }
            
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
                await _elasticsearchService.IndexDocumentAsync(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录审计日志失败");
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
            // 构建复合查询
            var searchFunc = BuildSearchQuery(query);
            
            // 添加分页和排序
            var combinedFunc = AuditQueryHelper.CombineQueries(
                searchFunc,
                AuditQueryHelper.CreatePaginationQuery(query.PageIndex, query.PageSize),
                AuditQueryHelper.CreateSortQuery("operationTime", false)
            );
            
            return await _elasticsearchService.SearchAsync<Models.AuditLog>(combinedFunc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索审计日志失败");
            return (Enumerable.Empty<Models.AuditLog>(), 0);
        }
    }
    
    /// <summary>
    /// 构建搜索查询
    /// </summary>
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> BuildSearchQuery(AuditLogQueryDto query)
    {
        var queries = new List<Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>>>();
        
        // 用户ID查询
        if (!string.IsNullOrEmpty(query.UserId))
        {
            queries.Add(AuditQueryHelper.CreateUserQuery(query.UserId));
        }
        
        // 操作类型查询
        if (!string.IsNullOrEmpty(query.OperationType))
        {
            queries.Add(AuditQueryHelper.CreateOperationQuery(query.OperationType));
        }
        
        // 时间范围查询
        if (query.StartTime.HasValue || query.EndTime.HasValue)
        {
            queries.Add(AuditQueryHelper.CreateTimeRangeQuery(query.StartTime, query.EndTime));
        }
        
        // 关键词搜索
        if (!string.IsNullOrEmpty(query.Keyword))
        {
            queries.Add(AuditQueryHelper.CreateTextQuery(query.Keyword));
        }
        
        // 如果没有任何查询条件，返回匹配所有的查询
        if (queries.Count == 0)
        {
            return s => s.From((query.PageIndex - 1) * query.PageSize)
                         .Size(query.PageSize);
        }
        
        // 组合所有查询条件
        return AuditQueryHelper.CombineQueries(queries.ToArray());
    }
    
    /// <summary>
    /// 获取操作统计
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            var aggregationFunc = CreateOperationStatsAggregation(startTime, endTime);
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(aggregationFunc);
            
            if (result != null)
            {
                return ParseOperationStatsResult(result);
            }
            
            return new Dictionary<string, long>();
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
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateOperationStatsAggregation(DateTime startTime, DateTime endTime)
    {
        return s => s
            .Size(0) // 不返回具体文档，只返回聚合结果
            .Query(q => q
                .Range(r => r
                    .DateRange(dr => dr
                        .Field(f => f.OperationTime)
                        .Gte(startTime)
                        .Lte(endTime)
                    )
                )
            )
            .Aggregations(a => a
                .Add("operations", agg => agg
                    .Terms(t => t
                        .Field(f => f.OperationType)
                        .Size(50)
                    )
                )
            );
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
                // 这里需要根据实际的聚合结果结构来解析
                // 简化处理，实际项目中需要更详细的解析逻辑
                _logger.LogDebug("操作统计结果: {Result}", operationsObj);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析操作统计结果失败");
        }
        
        return stats;
    }
    
    /// <summary>
    /// 获取用户统计
    /// </summary>
    public async Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10)
    {
        try
        {
            var aggregationFunc = CreateUserStatsAggregation(startTime, endTime, topN);
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(aggregationFunc);
            
            if (result != null)
            {
                return ParseUserStatsResult(result);
            }
            
            return new Dictionary<string, long>();
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
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateUserStatsAggregation(DateTime startTime, DateTime endTime, int topN)
    {
        return s => s
            .Size(0)
            .Query(q => q
                .Range(r => r
                    .DateRange(dr => dr
                        .Field(f => f.OperationTime)
                        .Gte(startTime)
                        .Lte(endTime)
                    )
                )
            )
            .Aggregations(a => a
                .Add("users", agg => agg
                    .Terms(t => t
                        .Field(f => f.UserId)
                        .Size(topN)
                    )
                )
            );
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
                // 这里需要根据实际的聚合结果结构来解析
                // 简化处理，实际项目中需要更详细的解析逻辑
                _logger.LogDebug("用户统计结果: {Result}", usersObj);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析用户统计结果失败");
        }
        
        return stats;
    }
    
    /// <summary>
    /// 获取操作趋势
    /// </summary>
    public async Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24)
    {
        try
        {
            var aggregationFunc = CreateOperationTrendAggregation(startTime, endTime, interval);
            var result = await _elasticsearchService.AggregateAsync<Models.AuditLog>(aggregationFunc);
            
            if (result != null)
            {
                return ParseOperationTrendResult(result);
            }
            
            return new Dictionary<DateTime, long>();
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
    private Func<SearchRequestDescriptor<Models.AuditLog>, SearchRequestDescriptor<Models.AuditLog>> CreateOperationTrendAggregation(DateTime startTime, DateTime endTime, int interval)
    {
        return s => s
            .Size(0)
            .Query(q => q
                .Range(r => r
                    .DateRange(dr => dr
                        .Field(f => f.OperationTime)
                        .Gte(startTime)
                        .Lte(endTime)
                    )
                )
            )
            .Aggregations(a => a
                .Add("trend", agg => agg
                    .DateHistogram(dh => dh
                        .Field(f => f.OperationTime)
                        .FixedInterval("1h")
                    )
                )
            );
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
                // 这里需要根据实际的聚合结果结构来解析
                // 简化处理，实际项目中需要更详细的解析逻辑
                _logger.LogDebug("操作趋势结果: {Result}", trendObj);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析操作趋势结果失败");
        }
        
        return trend;
    }
} 