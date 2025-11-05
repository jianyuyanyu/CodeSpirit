using CodeSpirit.Audit.Models.LLM;
using CodeSpirit.Audit.Services.LLM.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Audit.Services.LLM.Implementation;

/// <summary>
/// LLM审计Elasticsearch存储服务
/// </summary>
public class LLMElasticsearchStorageService : ILLMAuditStorageService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<LLMElasticsearchStorageService> _logger;
    private readonly LLMElasticsearchOptions _options;
    private readonly ElasticsearchClient _client;
    
    /// <summary>
    /// 初始化LLM审计Elasticsearch存储服务
    /// </summary>
    public LLMElasticsearchStorageService(
        IElasticsearchService elasticsearchService,
        ITenantContext? tenantContext,
        ILogger<LLMElasticsearchStorageService> logger,
        IOptions<Models.AuditOptions> auditOptions,
        ElasticsearchClient elasticsearchClient)
    {
        _elasticsearchService = elasticsearchService;
        _tenantContext = tenantContext;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit.Elasticsearch;
        _client = elasticsearchClient;
    }
    
    /// <summary>
    /// 获取索引名称
    /// </summary>
    private string GetIndexName()
    {
        if (string.IsNullOrWhiteSpace(_options.IndexPrefix))
        {
            return _options.IndexName;
        }
        
        return $"{_options.IndexPrefix}_{_options.IndexName}";
    }
    
    /// <inheritdoc/>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            var indexName = GetIndexName();
            
            // 检查索引是否存在
            var existsResponse = await _client.Indices.ExistsAsync(indexName);
            if (existsResponse.Exists)
            {
                _logger.LogInformation("LLM审计索引已存在: {IndexName}", indexName);
                return true;
            }
            
            // 创建索引
            var createResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(_options.NumberOfShards)
                    .NumberOfReplicas(_options.NumberOfReplicas)
                )
                .Mappings(m => m
                    .Properties<LLMAuditLog>(p => p
                        .Keyword(f => f.Id)
                        .Keyword(f => f.TenantId)
                        .Keyword(f => f.UserId)
                        .Text(f => f.UserName)
                        .Date(f => f.OperationTime)
                        .Keyword(f => f.LLMProvider)
                        .Keyword(f => f.ModelName)
                        .Keyword(f => f.InteractionType)
                        .Keyword(f => f.BusinessScenario)
                        .Text(f => f.SystemPrompt)
                        .Text(f => f.UserPrompt)
                        .Text(f => f.LLMResponse)
                        .Text(f => f.ProcessedData)
                        .LongNumber(f => f.ProcessingTimeMs)
                        .Boolean(f => f.IsSuccess)
                        .Text(f => f.ErrorMessage!)
                        .IntegerNumber(f => f.RetryCount)
                        .Boolean(f => f.WasJsonRepaired)
                        .IntegerNumber(f => f.QualityScore!.Value)
                        .Keyword(f => f.BatchId!)
                        .IntegerNumber(f => f.BatchSequence!.Value)
                        .Keyword(f => f.ParentAuditId!)
                        .Keyword(f => f.BusinessEntityId!)
                        .Keyword(f => f.BusinessEntityType!)
                        .IntegerNumber(f => f.DataCount!.Value)
                    )
                )
            );
            
            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("成功创建LLM审计索引: {IndexName}", indexName);
                return true;
            }
            else
            {
                _logger.LogError("创建LLM审计索引失败: {Error}", createResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化LLM审计索引时发生异常");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> StoreAsync(LLMAuditLog auditLog)
    {
        try
        {
            // 确保字符串不为null
            auditLog.SystemPrompt ??= string.Empty;
            auditLog.UserPrompt ??= string.Empty;
            auditLog.LLMResponse ??= string.Empty;
            auditLog.ProcessedData ??= string.Empty;
            auditLog.ErrorMessage ??= string.Empty;
            
            _logger.LogDebug("准备存储LLM审计日志到Elasticsearch: {Id}, 响应长度: {ResponseLength}", 
                auditLog.Id, auditLog.LLMResponse.Length);
            
            var indexName = GetIndexName();
            var indexResponse = await _client.IndexAsync(auditLog, idx => idx.Index(indexName));
            
            if (indexResponse.IsValidResponse)
            {
                _logger.LogDebug("LLM审计日志已成功存储到Elasticsearch: {Id}, 响应长度: {ResponseLength}", 
                    auditLog.Id, auditLog.LLMResponse.Length);
                return true;
            }
            else
            {
                _logger.LogError("存储LLM审计日志到Elasticsearch失败: {Error}", indexResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储LLM审计日志到Elasticsearch时发生异常");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> BulkStoreAsync(IEnumerable<LLMAuditLog> auditLogs)
    {
        try
        {
            var logsList = auditLogs.ToList();
            if (!logsList.Any())
            {
                return true;
            }
            
            // 确保字符串不为null
            foreach (var log in logsList)
            {
                log.SystemPrompt ??= string.Empty;
                log.UserPrompt ??= string.Empty;
                log.LLMResponse ??= string.Empty;
                log.ProcessedData ??= string.Empty;
                log.ErrorMessage ??= string.Empty;
            }
            
            var indexName = GetIndexName();
            var bulkResponse = await _client.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(logsList)
            );
            
            if (bulkResponse.IsValidResponse)
            {
                _logger.LogInformation("批量存储LLM审计日志到Elasticsearch成功: {Count}条", logsList.Count);
                return true;
            }
            else
            {
                _logger.LogError("批量存储LLM审计日志到Elasticsearch失败: {Error}", bulkResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储LLM审计日志到Elasticsearch时发生异常");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<LLMAuditLog?> GetByIdAsync(string id)
    {
        try
        {
            var indexName = GetIndexName();
            var getResponse = await _client.GetAsync<LLMAuditLog>(id, g => g.Index(indexName));
            
            if (getResponse.IsValidResponse && getResponse.Found)
            {
                return getResponse.Source;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取LLM审计日志时发生异常: {Id}", id);
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query)
    {
        try
        {
            var indexName = GetIndexName();
            
            // 构建查询条件
            var mustQueries = new List<Query>();
            
            // 租户过滤
            if (!string.IsNullOrEmpty(query.TenantId))
            {
                mustQueries.Add(new TermQuery(new Field("tenantId")) { Value = query.TenantId });
            }
            
            // 用户过滤
            if (!string.IsNullOrEmpty(query.UserId))
            {
                mustQueries.Add(new TermQuery(new Field("userId")) { Value = query.UserId });
            }
            
            // 模型过滤
            if (!string.IsNullOrEmpty(query.ModelName))
            {
                mustQueries.Add(new TermQuery(new Field("modelName")) { Value = query.ModelName });
            }
            
            // 交互类型过滤
            if (!string.IsNullOrEmpty(query.InteractionType))
            {
                mustQueries.Add(new TermQuery(new Field("interactionType")) { Value = query.InteractionType });
            }
            
            // 业务场景过滤
            if (!string.IsNullOrEmpty(query.BusinessScenario))
            {
                mustQueries.Add(new TermQuery(new Field("businessScenario")) { Value = query.BusinessScenario });
            }
            
            // 成功状态过滤
            if (query.IsSuccess.HasValue)
            {
                mustQueries.Add(new TermQuery(new Field("isSuccess")) { Value = query.IsSuccess.Value });
            }
            
            // 时间范围过滤
            if (query.StartTime.HasValue || query.EndTime.HasValue)
            {
                var rangeQuery = new DateRangeQuery(new Field("operationTime"));
                if (query.StartTime.HasValue)
                {
                    rangeQuery.Gte = DateMath.FromString(query.StartTime.Value.ToString("o"));
                }
                if (query.EndTime.HasValue)
                {
                    rangeQuery.Lte = DateMath.FromString(query.EndTime.Value.ToString("o"));
                }
                mustQueries.Add(rangeQuery);
            }
            
            // 批次ID过滤
            if (!string.IsNullOrEmpty(query.BatchId))
            {
                mustQueries.Add(new TermQuery(new Field("batchId")) { Value = query.BatchId });
            }
            
            // 业务实体过滤
            if (!string.IsNullOrEmpty(query.BusinessEntityType))
            {
                mustQueries.Add(new TermQuery(new Field("businessEntityType")) { Value = query.BusinessEntityType });
            }
            if (!string.IsNullOrEmpty(query.BusinessEntityId))
            {
                mustQueries.Add(new TermQuery(new Field("businessEntityId")) { Value = query.BusinessEntityId });
            }
            
            // 关键词搜索
            if (!string.IsNullOrEmpty(query.Keyword))
            {
                var shouldQueries = new List<Query>
                {
                    new MatchQuery(new Field("userPrompt")) { Query = query.Keyword },
                    new MatchQuery(new Field("llmResponse")) { Query = query.Keyword }
                };
                mustQueries.Add(new BoolQuery { Should = shouldQueries, MinimumShouldMatch = 1 });
            }
            
            // 执行搜索
            var searchResponse = await _client.SearchAsync<LLMAuditLog>(s => s
                .Index(indexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustQueries.ToArray())
                    )
                )
                .From((query.Page - 1) * query.PerPage)
                .Size(query.PerPage)
                .Sort(sort => sort
                    .Field(f => f.OperationTime, new FieldSort { Order = SortOrder.Desc })
                )
            );
            
            if (searchResponse.IsValidResponse)
            {
                var items = searchResponse.Documents ?? Enumerable.Empty<LLMAuditLog>();
                var total = searchResponse.Total;
                
                _logger.LogInformation("LLM审计日志搜索成功 - 返回 {ItemCount} 条记录，总计 {Total} 条", items.Count(), total);
                
                return (items, total);
            }
            else
            {
                _logger.LogError("LLM审计日志搜索失败: {Error}", searchResponse.DebugInformation);
                return (Enumerable.Empty<LLMAuditLog>(), 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索LLM审计日志时发生异常");
            return (Enumerable.Empty<LLMAuditLog>(), 0);
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, long>> GetAggregationAsync(string field, DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var indexName = GetIndexName();
            
            // 构建查询条件
            var mustQueries = new List<Query>();
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                mustQueries.Add(new TermQuery(new Field("tenantId")) { Value = tenantId });
            }
            
            mustQueries.Add(new DateRangeQuery(new Field("operationTime"))
            {
                Gte = DateMath.FromString(startTime.ToString("o")),
                Lte = DateMath.FromString(endTime.ToString("o"))
            });
            
            // 执行聚合查询
            var searchResponse = await _client.SearchAsync<LLMAuditLog>(s => s
                .Index(indexName)
                .Size(0)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustQueries.ToArray())
                    )
                )
                .Aggregations(aggs => aggs
                    .Add("field_agg", a => a
                        .Terms(t => t
                            .Field(new Field(field))
                            .Size(100)
                        )
                    )
                )
            );
            
            if (searchResponse.IsValidResponse && searchResponse.Aggregations != null)
            {
                var result = new Dictionary<string, long>();
                
                if (searchResponse.Aggregations.TryGetValue("field_agg", out var aggResult))
                {
                    // 处理Terms聚合结果
                    var bucketsAggregate = aggResult as Elastic.Clients.Elasticsearch.Aggregations.StringTermsAggregate;
                    if (bucketsAggregate?.Buckets != null)
                    {
                        foreach (var bucket in bucketsAggregate.Buckets)
                        {
                            if (bucket != null)
                            {
                                var keyStr = bucket.Key.ToString();
                                if (!string.IsNullOrEmpty(keyStr))
                                {
                                    result[keyStr] = bucket.DocCount;
                                }
                            }
                        }
                    }
                }
                
                return result;
            }
            
            return new Dictionary<string, long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM审计日志聚合统计时发生异常");
            return new Dictionary<string, long>();
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var healthResponse = await _client.PingAsync();
            return healthResponse.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM审计存储健康检查失败");
            return false;
        }
    }
}

