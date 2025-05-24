using Elastic.Clients.Elasticsearch;
using CodeSpirit.Audit.Models;
using System.Dynamic;
using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// Elasticsearch服务实现
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly ElasticsearchOptions _options;
    
    /// <summary>
    /// 构造函数 - 优先使用Aspire注入的客户端
    /// </summary>
    public ElasticsearchService(
        ILogger<ElasticsearchService> logger, 
        IConfiguration configuration,
        ElasticsearchClient? elasticsearchClient = null)
    {
        _logger = logger;
        
        // 获取配置 - 智能处理配置绑定
        var options = new AuditOptions();
        if (configuration.GetSection("Audit").Exists())
        {
            // 传入的是完整配置，获取Audit节
            configuration.GetSection("Audit").Bind(options);
        }
        else
        {
            // 传入的就是Audit配置节
            configuration.Bind(options);
        }
        _options = options.Elasticsearch;
        
        if (elasticsearchClient != null)
        {
            // 使用Aspire注入的客户端
            _client = elasticsearchClient;
            _logger.LogInformation("使用Aspire配置的Elasticsearch客户端");
        }
        else
        {
            // 回退到手动创建客户端
            _client = CreateManualClient();
            _logger.LogInformation("使用手动配置的Elasticsearch客户端");
        }
    }
    
    /// <summary>
    /// 手动创建Elasticsearch客户端
    /// </summary>
    private ElasticsearchClient CreateManualClient()
    {
        try
        {
            // 创建连接设置
            var settings = new ElasticsearchClientSettings();
            
            // 设置节点地址
            if (_options.Urls?.Any() == true)
            {
                var uri = new Uri(_options.Urls.First());
                settings = new ElasticsearchClientSettings(uri);
            }
            
            // 设置默认索引
            settings = settings.DefaultIndex(_options.IndexName);
            
            // 如果配置了用户名和密码，则设置基本认证
            if (!string.IsNullOrEmpty(_options.UserName) && !string.IsNullOrEmpty(_options.Password))
            {
                settings = settings.Authentication(new Elastic.Transport.BasicAuthentication(_options.UserName, _options.Password));
            }
            
            // 创建客户端
            var client = new ElasticsearchClient(settings);
            
            _logger.LogInformation("Elasticsearch手动连接已建立");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch连接建立失败");
            throw;
        }
    }
    
    /// <summary>
    /// 重建索引（删除现有索引并重新创建）
    /// </summary>
    public async Task<bool> RecreateIndexAsync()
    {
        try
        {
            // 检查索引是否存在，如果存在则删除
            if (await IndexExistsAsync())
            {
                _logger.LogInformation("正在删除现有Elasticsearch索引: {IndexName}", _options.IndexName);
                var deleteResponse = await _client.Indices.DeleteAsync(_options.IndexName);
                
                if (!deleteResponse.IsValidResponse)
                {
                    _logger.LogError("删除现有Elasticsearch索引失败: {Error}", deleteResponse.DebugInformation);
                    return false;
                }
                
                _logger.LogInformation("现有Elasticsearch索引删除成功: {IndexName}", _options.IndexName);
            }
            
            // 等待一小段时间确保删除操作完成
            await Task.Delay(1000);
            
            // 重新创建索引
            return await CreateIndexAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重建Elasticsearch索引失败");
            return false;
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
            
            // 创建索引，使用明确的字段映射以确保正确的字段名称
            var createResponse = await _client.Indices.CreateAsync(_options.IndexName, c => c
                .Settings(s => s
                    .NumberOfShards(_options.NumberOfShards)
                    .NumberOfReplicas(_options.NumberOfReplicas)
                )
                .Mappings(m => m
                    .Properties<AuditLog>(p => p
                        .Keyword(f => f.Id)
                        .Keyword(f => f.UserId)
                        .Text(f => f.UserName)
                        .Keyword(f => f.IpAddress)
                        .Date(f => f.OperationTime) // 这将映射为operationTime字段
                        .Keyword(f => f.ServiceName)
                        .Keyword(f => f.ControllerName)
                        .Keyword(f => f.ActionName)
                        .Keyword(f => f.OperationType)
                        .Text(f => f.Description)
                        .Keyword(f => f.RequestPath)
                        .Keyword(f => f.RequestMethod)
                        .Text(f => f.RequestParams)
                        .Keyword(f => f.EntityName)
                        .Keyword(f => f.EntityId)
                        .LongNumber(f => f.ExecutionDuration)
                        .Boolean(f => f.IsSuccess)
                        .Text(f => f.ErrorMessage)
                        .IntegerNumber(f => f.StatusCode)
                        .Text(f => f.BeforeData)
                        .Text(f => f.AfterData)
                        .Text(f => f.UserAgent)
                        .Text(f => f.OperationName)
                    )
                )
            );
            
            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Elasticsearch索引创建成功，包含正确的字段映射: {IndexName}", _options.IndexName);
                
                // 记录字段映射信息以便调试
                _logger.LogDebug("索引字段映射创建完成，OperationTime字段将映射为operationTime");
                return true;
            }
            else
            {
                _logger.LogError("Elasticsearch索引创建失败: {Error}", createResponse.DebugInformation);
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
            return existsResponse.IsValidResponse;
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
            
            var indexResponse = await _client.IndexAsync(document, idx => idx.Index(_options.IndexName));
            
            if (indexResponse.IsValidResponse)
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
        try
        {
            if (!documents.Any())
            {
                return true;
            }

            // 确保索引存在
            if (!await IndexExistsAsync())
            {
                await CreateIndexAsync();
            }

            // 使用简单的批量索引
            var bulkResponse = await _client.BulkAsync(b => b
                .Index(_options.IndexName)
                .IndexMany(documents)
            );

            if (bulkResponse.IsValidResponse && !bulkResponse.Errors)
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
            
            if (getResponse.IsValidResponse && getResponse.Found)
            {
                return getResponse.Source;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Elasticsearch获取文档失败");
            return null;
        }
    }
    
    /// <summary>
    /// 搜索文档
    /// </summary>
    public async Task<(IEnumerable<T> Items, long Total)> SearchAsync<T>(Func<SearchRequestDescriptor<T>, SearchRequestDescriptor<T>> searchFunc) where T : class
    {
        try
        {
            var searchDescriptor = new SearchRequestDescriptor<T>().Index(_options.IndexName);
            var searchRequest = searchFunc(searchDescriptor);
            
            // 记录搜索请求信息
            _logger.LogInformation("开始执行Elasticsearch搜索，索引: {IndexName}", _options.IndexName);
            
            var searchResponse = await _client.SearchAsync<T>(searchRequest);
            
            // 详细记录搜索结果
            _logger.LogInformation("Elasticsearch搜索完成");
            _logger.LogInformation("搜索响应状态: {IsValid}", searchResponse.IsValidResponse);
            _logger.LogInformation("返回文档数量: {DocumentCount}", searchResponse.Documents?.Count() ?? 0);
            _logger.LogInformation("总文档数量: {Total}", searchResponse.Total);
            _logger.LogInformation("查询耗时: {Took}ms", searchResponse.Took);
            
            if (searchResponse.IsValidResponse)
            {
                var items = searchResponse.Documents ?? Enumerable.Empty<T>();
                var total = searchResponse.Total;
                
                _logger.LogInformation("搜索成功 - 返回 {ItemCount} 条记录，总计 {Total} 条", items.Count(), total);
                
                // 如果是AuditLog类型，打印前几条记录的关键信息
                if (typeof(T) == typeof(CodeSpirit.Audit.Models.AuditLog))
                {
                    var auditLogs = items.Cast<CodeSpirit.Audit.Models.AuditLog>().Take(3);
                    foreach (var log in auditLogs)
                    {
                        _logger.LogInformation("审计日志样本 - ID: {Id}, 用户: {UserName}, 操作时间: {OperationTime}, 操作类型: {OperationType}", 
                            log.Id, log.UserName, log.OperationTime, log.OperationType);
                    }
                }
                
                return (items, total);
            }
            else
            {
                _logger.LogError("Elasticsearch搜索失败");
                _logger.LogError("错误信息: {Error}", searchResponse.DebugInformation);
                
                // 记录HTTP状态码和基本错误信息
                _logger.LogError("HTTP状态码: {StatusCode}", searchResponse.ApiCallDetails?.HttpStatusCode);
                
                if (searchResponse.ElasticsearchServerError != null)
                {
                    _logger.LogError("服务器错误详情: {ServerError}", searchResponse.ElasticsearchServerError.ToString());
                }
                
                return (Enumerable.Empty<T>(), 0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch搜索时发生异常");
            _logger.LogError("异常类型: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("异常消息: {Message}", ex.Message);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("内部异常: {InnerMessage}", ex.InnerException.Message);
            }
            
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
            var deleteResponse = await _client.DeleteAsync<object>(id, d => d.Index(_options.IndexName));
            
            if (deleteResponse.IsValidResponse)
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
    public async Task<IDictionary<string, object>?> AggregateAsync<T>(Func<SearchRequestDescriptor<T>, SearchRequestDescriptor<T>> aggregationFunc) where T : class
    {
        try
        {
            var searchDescriptor = new SearchRequestDescriptor<T>().Index(_options.IndexName).Size(0);
            var searchRequest = aggregationFunc(searchDescriptor);
            
            var searchResponse = await _client.SearchAsync<T>(searchRequest);
            
            if (searchResponse.IsValidResponse && searchResponse.Aggregations != null)
            {
                // 简化的聚合结果处理
                var result = new Dictionary<string, object>();
                foreach (var agg in searchResponse.Aggregations)
                {
                    result[agg.Key] = agg.Value;
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
            _logger.LogError(ex, "Elasticsearch聚合查询失败");
            return null;
        }
    }
} 