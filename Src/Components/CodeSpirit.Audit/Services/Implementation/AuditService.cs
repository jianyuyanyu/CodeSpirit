using Nest;

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
            
            // 将审计日志推送到RabbitMQ
            await _rabbitMQService.SendMessageAsync(auditLog);
            
            _logger.LogDebug("审计日志已推送到消息队列: {Id}", auditLog.Id);
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
            // 构建Elasticsearch查询
            Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> searchDescriptorFunc = d =>
            {
                var searchDescriptor = d
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Sort(s =>
                    {
                        if (query.SortDirection.ToLower() == "asc")
                        {
                            return s.Ascending(query.SortField);
                        }
                        else
                        {
                            return s.Descending(query.SortField);
                        }
                    });
                
                var queryContainer = new List<QueryContainer>();
                
                // 用户ID
                if (!string.IsNullOrEmpty(query.UserId))
                {
                    queryContainer.Add(new TermQuery { Field = "userId.keyword", Value = query.UserId });
                }
                
                // 用户名
                if (!string.IsNullOrEmpty(query.UserName))
                {
                    queryContainer.Add(new MatchPhraseQuery { Field = "userName", Query = query.UserName });
                }
                
                // IP地址
                if (!string.IsNullOrEmpty(query.IpAddress))
                {
                    queryContainer.Add(new TermQuery { Field = "ipAddress.keyword", Value = query.IpAddress });
                }
                
                // 时间范围
                if (query.StartTime.HasValue || query.EndTime.HasValue)
                {
                    var rangeQuery = new DateRangeQuery { Field = "operationTime" };
                    
                    if (query.StartTime.HasValue)
                    {
                        rangeQuery.GreaterThanOrEqualTo = query.StartTime.Value;
                    }
                    
                    if (query.EndTime.HasValue)
                    {
                        rangeQuery.LessThanOrEqualTo = query.EndTime.Value;
                    }
                    
                    queryContainer.Add(rangeQuery);
                }
                
                // 服务名称
                if (!string.IsNullOrEmpty(query.ServiceName))
                {
                    queryContainer.Add(new TermQuery { Field = "serviceName.keyword", Value = query.ServiceName });
                }
                
                // 控制器名称
                if (!string.IsNullOrEmpty(query.ControllerName))
                {
                    queryContainer.Add(new TermQuery { Field = "controllerName.keyword", Value = query.ControllerName });
                }
                
                // 操作名称
                if (!string.IsNullOrEmpty(query.ActionName))
                {
                    queryContainer.Add(new TermQuery { Field = "actionName.keyword", Value = query.ActionName });
                }
                
                // 操作类型
                if (!string.IsNullOrEmpty(query.OperationType))
                {
                    queryContainer.Add(new TermQuery { Field = "operationType.keyword", Value = query.OperationType });
                }
                
                // 实体名称
                if (!string.IsNullOrEmpty(query.EntityName))
                {
                    queryContainer.Add(new TermQuery { Field = "entityName.keyword", Value = query.EntityName });
                }
                
                // 实体ID
                if (!string.IsNullOrEmpty(query.EntityId))
                {
                    queryContainer.Add(new TermQuery { Field = "entityId.keyword", Value = query.EntityId });
                }
                
                // 是否成功
                if (query.IsSuccess.HasValue)
                {
                    queryContainer.Add(new TermQuery { Field = "isSuccess", Value = query.IsSuccess.Value });
                }
                
                // 关键字搜索
                if (!string.IsNullOrEmpty(query.Keyword))
                {
                    queryContainer.Add(new MultiMatchQuery
                    {
                        Fields = new[] { "description", "requestParams", "errorMessage", "beforeData", "afterData" },
                        Query = query.Keyword,
                        Type = TextQueryType.BestFields,
                        Operator = Operator.Or
                    });
                }
                
                // 应用查询条件
                if (queryContainer.Any())
                {
                    searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b.Must(queryContainer.ToArray())));
                }
                
                return searchDescriptor;
            };
            
            return await _elasticsearchService.SearchAsync<Models.AuditLog>(searchDescriptorFunc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索审计日志失败");
            return (Enumerable.Empty<Models.AuditLog>(), 0);
        }
    }
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            // 创建聚合查询
            Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> aggregationFunc = s => s
                .Query(q => q
                    .DateRange(r => r
                        .Field(f => f.OperationTime)
                        .GreaterThanOrEquals(startTime)
                        .LessThanOrEquals(endTime)
                    )
                )
                .Aggregations(a => a
                    .Terms("operations", t => t
                        .Field(f => f.OperationType.Suffix("keyword"))
                        .Size(20)
                    )
                );
            
            // 执行聚合查询
            var result = await _elasticsearchService.AggregateAsync<AuditLog>(aggregationFunc);
            
            // 解析结果
            var stats = new Dictionary<string, long>();
            
            if (result != null && result.TryGetValue("operations", out var operationsObj))
            {
                var operations = operationsObj as List<Dictionary<string, object>>;
                if (operations != null)
                {
                    foreach (var operation in operations)
                    {
                        if (operation.TryGetValue("key", out var keyObj) && operation.TryGetValue("count", out var countObj))
                        {
                            var key = keyObj?.ToString();
                            var count = Convert.ToInt64(countObj);
                            
                            if (!string.IsNullOrEmpty(key))
                            {
                                stats[key] = count;
                            }
                        }
                    }
                }
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作统计信息失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    public async Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10)
    {
        try
        {
            // 创建聚合查询
            Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> aggregationFunc = s => s
                .Query(q => q
                    .DateRange(r => r
                        .Field(f => f.OperationTime)
                        .GreaterThanOrEquals(startTime)
                        .LessThanOrEquals(endTime)
                    )
                )
                .Aggregations(a => a
                    .Terms("users", t => t
                        .Field(f => f.UserName.Suffix("keyword"))
                        .Size(topN)
                    )
                );
            
            // 执行聚合查询
            var result = await _elasticsearchService.AggregateAsync<AuditLog>(aggregationFunc);
            
            // 解析结果
            var stats = new Dictionary<string, long>();
            
            if (result != null && result.TryGetValue("users", out var usersObj))
            {
                var users = usersObj as List<Dictionary<string, object>>;
                if (users != null)
                {
                    foreach (var user in users)
                    {
                        if (user.TryGetValue("key", out var keyObj) && user.TryGetValue("count", out var countObj))
                        {
                            var key = keyObj?.ToString();
                            var count = Convert.ToInt64(countObj);
                            
                            if (!string.IsNullOrEmpty(key))
                            {
                                stats[key] = count;
                            }
                        }
                    }
                }
            }
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计信息失败");
            return new Dictionary<string, long>();
        }
    }
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    public async Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24)
    {
        try
        {
            // 计算时间间隔
            string intervalStr = $"{interval}h";
            
            // 创建聚合查询
            Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>> aggregationFunc = s => s
                .Query(q => q
                    .DateRange(r => r
                        .Field(f => f.OperationTime)
                        .GreaterThanOrEquals(startTime)
                        .LessThanOrEquals(endTime)
                    )
                )
                .Aggregations(a => a
                    .DateHistogram("trend", h => h
                        .Field(f => f.OperationTime)
                        .CalendarInterval(intervalStr)
                        .Format("yyyy-MM-dd'T'HH:mm:ss")
                        .MinimumDocumentCount(0)
                        .ExtendedBounds(
                            startTime,
                            endTime
                        )
                    )
                );
            
            // 执行聚合查询
            var result = await _elasticsearchService.AggregateAsync<AuditLog>(aggregationFunc);
            
            // 解析结果
            var trend = new Dictionary<DateTime, long>();
            
            if (result != null && result.TryGetValue("trend", out var trendObj))
            {
                var trendData = trendObj as List<Dictionary<string, object>>;
                if (trendData != null)
                {
                    foreach (var point in trendData)
                    {
                        if (point.TryGetValue("key", out var keyObj) && point.TryGetValue("count", out var countObj))
                        {
                            var dateString = keyObj?.ToString();
                            if (!string.IsNullOrEmpty(dateString))
                            {
                                var date = DateTime.ParseExact(dateString, "yyyy-MM-dd'T'HH:mm:ss", null);
                                trend[date] = Convert.ToInt64(countObj);
                            }
                        }
                    }
                }
            }
            
            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作趋势信息失败");
            return new Dictionary<DateTime, long>();
        }
    }
} 