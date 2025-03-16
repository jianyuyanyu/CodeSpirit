using Nest;
using Elasticsearch.Net;
using CodeSpirit.Audit.Models;
using System.Dynamic;
using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// Elasticsearch服务实现
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly ElasticsearchOptions _options;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ElasticsearchService(ILogger<ElasticsearchService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options.Elasticsearch;
        
        try
        {
            // 创建连接设置
            var pool = new StaticConnectionPool(_options.Urls.Select(url => new Uri(url)));
            var settings = new ConnectionSettings(pool);
            
            // 设置默认索引
            settings.DefaultIndex(_options.IndexName);
            
            // 如果配置了用户名和密码，则设置基本认证
            if (!string.IsNullOrEmpty(_options.UserName) && !string.IsNullOrEmpty(_options.Password))
            {
                settings.BasicAuthentication(_options.UserName, _options.Password);
            }
            
            // 设置最大重试次数
            settings.MaximumRetries(5);
            
            // 创建客户端
            _client = new ElasticClient(settings);
            
            _logger.LogInformation("Elasticsearch连接已建立");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch连接建立失败");
            throw;
        }
    }
    
    /// <summary>
    /// 创建索引
    /// </summary>
    public async Task<bool> CreateIndexAsync()
    {
        try
        {
            // 检查索引是否存在
            if (await IndexExistsAsync())
            {
                _logger.LogInformation("Elasticsearch索引已存在: {IndexName}", _options.IndexName);
                return true;
            }
            
            // 创建索引
            var createIndexResponse = await _client.Indices.CreateAsync(_options.IndexName, c => c
                .Settings(s => s
                    .NumberOfShards(_options.NumberOfShards)
                    .NumberOfReplicas(_options.NumberOfReplicas)
                    .Analysis(a => a
                        .Analyzers(aa => aa
                            .Custom("standard_pinyin", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "asciifolding")
                            )
                        )
                    )
                )
                .Map<AuditLog>(m => m
                    .AutoMap() // 自动映射属性
                    .Properties(p => p
                        .Date(d => d.Name(n => n.OperationTime).Format("yyyy-MM-dd'T'HH:mm:ss.SSSZ"))
                        .Text(t => t.Name(n => n.UserId).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.UserName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.IpAddress).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.UserAgent).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Object<GeoLoc>(o => o
                            .Name(n => n.Location)
                            .Properties(lp => lp
                                .Text(t => t.Name(n => n.Country).Fields(f => f.Keyword(k => k.Name("keyword"))))
                                .Text(t => t.Name(n => n.CountryCode).Fields(f => f.Keyword(k => k.Name("keyword"))))
                                .Text(t => t.Name(n => n.Region).Fields(f => f.Keyword(k => k.Name("keyword"))))
                                .Text(t => t.Name(n => n.City).Fields(f => f.Keyword(k => k.Name("keyword"))))
                                .GeoPoint(g => g.Name(n => n.Longitude))
                                .GeoPoint(g => g.Name(n => n.Latitude))
                                .Text(t => t.Name(n => n.ISP).Fields(f => f.Keyword(k => k.Name("keyword"))))
                            )
                        )
                        .Text(t => t.Name(n => n.ServiceName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.ControllerName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.ActionName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.OperationName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.OperationType).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.Description))
                        .Text(t => t.Name(n => n.RequestPath).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.RequestMethod).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.RequestParams))
                        .Text(t => t.Name(n => n.EntityName).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.EntityId).Fields(f => f.Keyword(k => k.Name("keyword"))))
                        .Text(t => t.Name(n => n.BeforeData))
                        .Text(t => t.Name(n => n.AfterData))
                        .Number(n => n.Name(n => n.ExecutionDuration).Type(NumberType.Long))
                        .Boolean(b => b.Name(n => n.IsSuccess))
                        .Text(t => t.Name(n => n.ErrorMessage))
                        .Object<Dictionary<string, string>>(o => o.Name(n => n.AttributeProperties))
                        .Object<Dictionary<string, object>>(o => o.Name(n => n.AdditionalData))
                    )
                )
            );
            
            if (createIndexResponse.IsValid)
            {
                _logger.LogInformation("Elasticsearch索引创建成功: {IndexName}", _options.IndexName);
                return true;
            }
            else
            {
                _logger.LogError("Elasticsearch索引创建失败: {Error}", createIndexResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Elasticsearch索引失败");
            return false;
        }
    }
    
    /// <summary>
    /// 检查索引是否存在
    /// </summary>
    public async Task<bool> IndexExistsAsync()
    {
        try
        {
            var existsResponse = await _client.Indices.ExistsAsync(_options.IndexName);
            return existsResponse.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Elasticsearch索引是否存在失败");
            return false;
        }
    }
    
    /// <summary>
    /// 索引文档
    /// </summary>
    public async Task<bool> IndexDocumentAsync<T>(T document) where T : class
    {
        try
        {
            // 确保索引存在
            if (!await IndexExistsAsync())
            {
                await CreateIndexAsync();
            }
            
            var indexResponse = await _client.IndexDocumentAsync(document);
            
            if (indexResponse.IsValid)
            {
                _logger.LogDebug("文档已成功索引到Elasticsearch: {Id}", indexResponse.Id);
                return true;
            }
            else
            {
                _logger.LogError("索引文档到Elasticsearch失败: {Error}", indexResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "索引文档到Elasticsearch失败");
            return false;
        }
    }
    
    /// <summary>
    /// 批量索引文档
    /// </summary>
    public async Task<bool> BulkIndexAsync<T>(IEnumerable<T> documents) where T : class
    {
        if (documents == null || !documents.Any())
        {
            return true;
        }
        
        try
        {
            // 确保索引存在
            if (!await IndexExistsAsync())
            {
                await CreateIndexAsync();
            }
            
            var bulkRequest = new BulkRequest(_options.IndexName)
            {
                Operations = new List<IBulkOperation>()
            };
            
            foreach (var document in documents)
            {
                bulkRequest.Operations.Add(new BulkIndexOperation<T>(document));
            }
            
            var bulkResponse = await _client.BulkAsync(bulkRequest);
            
            if (bulkResponse.IsValid)
            {
                _logger.LogInformation("批量索引文档到Elasticsearch成功: {Count}条", documents.Count());
                return true;
            }
            else
            {
                _logger.LogError("批量索引文档到Elasticsearch失败: {Error}", bulkResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量索引文档到Elasticsearch失败");
            return false;
        }
    }
    
    /// <summary>
    /// 获取文档
    /// </summary>
    public async Task<T?> GetDocumentAsync<T>(string id) where T : class
    {
        try
        {
            var getResponse = await _client.GetAsync<T>(id, g => g.Index(_options.IndexName));
            
            if (getResponse.IsValid)
            {
                return getResponse.Source;
            }
            else
            {
                _logger.LogError("Elasticsearch获取文档失败: {Error}", getResponse.DebugInformation);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文档时发生错误");
            _logger.LogError(ex, "从Elasticsearch获取文档失败");
            return null;
        }
    }
    
    /// <summary>
    /// 搜索文档
    /// </summary>
    public async Task<(IEnumerable<T> Items, long Total)> SearchAsync<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> searchFunc) where T : class
    {
        try
        {
            var searchRequest = searchFunc(new SearchDescriptor<T>().Index(_options.IndexName));
            var searchResponse = await _client.SearchAsync<T>(searchRequest);
            
            if (searchResponse.IsValid)
            {
                return (searchResponse.Documents, searchResponse.Total);
            }
            
            _logger.LogError("Elasticsearch搜索失败: {Error}", searchResponse.DebugInformation);
            return (Enumerable.Empty<T>(), 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索文档时发生错误");
            return (Enumerable.Empty<T>(), 0);
        }
    }
    
    /// <summary>
    /// 删除文档
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string id)
    {
        try
        {
            var deleteResponse = await _client.DeleteAsync(new DeleteRequest(_options.IndexName, id));
            
            if (deleteResponse.IsValid)
            {
                _logger.LogInformation("从Elasticsearch删除文档成功: {Id}", id);
                return true;
            }
            else
            {
                _logger.LogError("从Elasticsearch删除文档失败: {Error}", deleteResponse.DebugInformation);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Elasticsearch删除文档失败");
            return false;
        }
    }
    
    /// <summary>
    /// 聚合查询
    /// </summary>
    public async Task<IDictionary<string, object>?> AggregateAsync<T>(Func<SearchDescriptor<T>, SearchDescriptor<T>> aggregationFunc) where T : class
    {
        try
        {
            var searchRequest = aggregationFunc(new SearchDescriptor<T>().Index(_options.IndexName));
            var searchResponse = await _client.SearchAsync<T>(searchRequest);
            
            if (searchResponse.IsValid && searchResponse.Aggregations != null && searchResponse.Aggregations.Count > 0)
            {
                var result = new Dictionary<string, object>();
                
                // 将聚合结果转换为字典对象
                foreach (var aggregation in searchResponse.Aggregations)
                {
                    var convertedValue = ConvertAggregation(aggregation.Value);
                    if (convertedValue != null)
                    {
                        result[aggregation.Key] = convertedValue;
                    }
                }
                
                return result;
            }
            else
            {
                _logger.LogError("Elasticsearch聚合查询失败: {Error}", searchResponse.DebugInformation);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚合查询时发生错误");
            return null;
        }
    }
    
    /// <summary>
    /// 转换聚合结果
    /// </summary>
    private object ConvertAggregation(IAggregate aggregate)
    {
        if (aggregate is ValueAggregate valueAggregate)
        {
            return valueAggregate.Value;
        }
        else if (aggregate is BucketAggregate bucketAggregate)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var bucket in bucketAggregate.Items)
            {
                var bucketResult = new Dictionary<string, object>();
                
                // 键值桶
                if (bucket is KeyedBucket<object> keyedBucket)
                {
                    bucketResult["key"] = keyedBucket.Key;
                    bucketResult["count"] = keyedBucket.DocCount;
                }
                
                result.Add(bucketResult);
            }
            return result;
        }
        else if (aggregate is SingleBucketAggregate singleBucketAggregate)
        {
            var result = new Dictionary<string, object>
            {
                ["count"] = singleBucketAggregate.DocCount
            };
            
            return result;
        }
        
        return null;
    }
    
    private IDictionary<string, object> ConvertAggregations(IReadOnlyDictionary<string, IAggregate> aggregations)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var agg in aggregations)
        {
            result[agg.Key] = ConvertAggregation(agg.Value);
        }
        
        return result;
    }
} 