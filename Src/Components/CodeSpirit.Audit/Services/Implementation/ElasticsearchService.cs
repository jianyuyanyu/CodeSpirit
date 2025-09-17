using Elastic.Clients.Elasticsearch;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Helpers;
using System.Dynamic;
using GeoLoc = CodeSpirit.Audit.Models.GeoLocation;
using System.Text;

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
    /// 获取最终的索引名称（包含前缀）
    /// </summary>
    /// <returns>完整的索引名称</returns>
    private string GetFinalIndexName()
    {
        if (string.IsNullOrWhiteSpace(_options.IndexPrefix))
        {
            return _options.IndexName;
        }
        
        return $"{_options.IndexPrefix}_{_options.IndexName}";
    }
    
    /// <summary>
    /// 构造函数 - 优先使用Aspire注入的客户端
    /// </summary>
    public ElasticsearchService(
        ILogger<ElasticsearchService> logger, 
        IConfiguration configuration,
        ElasticsearchClient? elasticsearchClient = null)
    {
        _logger = logger;
        
        // 使用配置助手简化配置绑定
        _options = ConfigurationHelper.BindElasticsearchOptions(configuration);
        
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
            settings = settings.DefaultIndex(GetFinalIndexName());
            
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
                _logger.LogInformation("正在删除现有Elasticsearch索引: {IndexName}", GetFinalIndexName());
                var deleteResponse = await _client.Indices.DeleteAsync(GetFinalIndexName());
                
                if (!deleteResponse.IsValidResponse)
                {
                    _logger.LogError("删除现有Elasticsearch索引失败: {Error}", deleteResponse.DebugInformation);
                    return false;
                }
                
                _logger.LogInformation("现有Elasticsearch索引删除成功: {IndexName}", GetFinalIndexName());
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
                _logger.LogInformation("Elasticsearch索引已存在: {IndexName}", GetFinalIndexName());
                return true;
            }
            
            // 创建索引，使用明确的字段映射以确保正确的字段名称
            var createResponse = await _client.Indices.CreateAsync(GetFinalIndexName(), c => c
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
                        .Keyword(f => f.OperationType)
                        .Text(f => f.Description)
                        .Keyword(f => f.RequestPath)
                        .Text(f => f.RequestParams)
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
                _logger.LogInformation("Elasticsearch索引创建成功，包含正确的字段映射: {IndexName}", GetFinalIndexName());
                
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
            var existsResponse = await _client.Indices.ExistsAsync(GetFinalIndexName());
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
            
            var indexResponse = await _client.IndexAsync(document, idx => idx.Index(GetFinalIndexName()));
            
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
                .Index(GetFinalIndexName())
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
            var getResponse = await _client.GetAsync<T>(id, g => g.Index(GetFinalIndexName()));
            
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
            var searchDescriptor = new SearchRequestDescriptor<T>().Index(GetFinalIndexName());
            var searchRequest = searchFunc(searchDescriptor);
            
            // 记录搜索请求信息
            _logger.LogInformation("开始执行Elasticsearch搜索，索引: {IndexName}", GetFinalIndexName());
            
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
            var deleteResponse = await _client.DeleteAsync<object>(id, d => d.Index(GetFinalIndexName()));
            
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
            var searchDescriptor = new SearchRequestDescriptor<T>().Index(GetFinalIndexName()).Size(0);
            var searchRequest = aggregationFunc(searchDescriptor);
            
            _logger.LogInformation("开始执行Elasticsearch聚合查询，索引: {IndexName}", GetFinalIndexName());
            
            var searchResponse = await _client.SearchAsync<T>(searchRequest);
            
            _logger.LogInformation("Elasticsearch聚合查询完成");
            _logger.LogInformation("聚合响应状态: {IsValid}", searchResponse.IsValidResponse);
            _logger.LogInformation("查询耗时: {Took}ms", searchResponse.Took);
            
            if (searchResponse.IsValidResponse && searchResponse.Aggregations != null)
            {
                _logger.LogInformation("聚合查询成功，返回 {Count} 个聚合结果", searchResponse.Aggregations.Count);
                
                // 详细的聚合结果处理
                var result = new Dictionary<string, object>();
                
                foreach (var agg in searchResponse.Aggregations)
                {
                    var aggregationResult = ParseAggregationResult(agg.Key, agg.Value);
                    result[agg.Key] = aggregationResult;
                    
                    _logger.LogDebug("聚合结果 {AggName}: {Result}", agg.Key, 
                        System.Text.Json.JsonSerializer.Serialize(aggregationResult));
                }
                
                return result;
            }
            else
            {
                _logger.LogError("Elasticsearch聚合查询失败");
                _logger.LogError("错误信息: {Error}", searchResponse.DebugInformation);
                
                if (searchResponse.ElasticsearchServerError != null)
                {
                    _logger.LogError("服务器错误详情: {ServerError}", searchResponse.ElasticsearchServerError.ToString());
                }
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Elasticsearch聚合查询时发生异常");
            _logger.LogError("异常类型: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("异常消息: {Message}", ex.Message);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("内部异常: {InnerMessage}", ex.InnerException.Message);
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// 解析聚合结果
    /// </summary>
    /// <param name="aggregationName">聚合名称</param>
    /// <param name="aggregationValue">聚合值</param>
    /// <returns>解析后的聚合结果</returns>
    private object ParseAggregationResult(string aggregationName, object aggregationValue)
    {
        try
        {
            // 使用动态类型处理，避免直接引用具体的聚合类型
            var aggregationType = aggregationValue.GetType();
            var typeName = aggregationType.Name;
            
            _logger.LogDebug("处理聚合类型: {TypeName}", typeName);
            
            // 根据类型名称进行处理
            return typeName switch
            {
                var name when name.Contains("Terms") => ParseTermsAggregationDynamic(aggregationValue),
                var name when name.Contains("DateHistogram") => ParseDateHistogramAggregationDynamic(aggregationValue),
                var name when name.Contains("Value") => ParseValueAggregationDynamic(aggregationValue),
                var name when name.Contains("Stats") => ParseStatsAggregationDynamic(aggregationValue),
                var name when name.Contains("Range") => ParseRangeAggregationDynamic(aggregationValue),
                _ => ParseGenericAggregationDynamic(aggregationValue)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析聚合结果失败，聚合名称: {AggName}", aggregationName);
            return aggregationValue; // 返回原始值作为备选
        }
    }
    
    /// <summary>
    /// 动态解析Terms聚合结果
    /// </summary>
    private object ParseTermsAggregationDynamic(object termsAgg)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var type = termsAgg.GetType();
            
            // 获取Buckets属性
            var bucketsProperty = type.GetProperty("Buckets");
            if (bucketsProperty != null)
            {
                var buckets = bucketsProperty.GetValue(termsAgg);
                if (buckets is IEnumerable<object> bucketList)
                {
                    var bucketResults = new List<Dictionary<string, object>>();
                    
                    foreach (var bucket in bucketList)
                    {
                        var bucketData = ParseBucketDynamic(bucket);
                        bucketResults.Add(bucketData);
                    }
                    
                    result["buckets"] = bucketResults;
                }
            }
            
            // 获取其他统计信息
            var docCountErrorProperty = type.GetProperty("DocCountErrorUpperBound");
            if (docCountErrorProperty != null)
            {
                result["doc_count_error_upper_bound"] = docCountErrorProperty.GetValue(termsAgg) ?? 0;
            }
            
            var sumOtherDocCountProperty = type.GetProperty("SumOtherDocCount");
            if (sumOtherDocCountProperty != null)
            {
                result["sum_other_doc_count"] = sumOtherDocCountProperty.GetValue(termsAgg) ?? 0;
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析Terms聚合失败");
            return new Dictionary<string, object> { ["error"] = "解析失败" };
        }
    }
    
    /// <summary>
    /// 动态解析日期直方图聚合结果
    /// </summary>
    private object ParseDateHistogramAggregationDynamic(object dateHistAgg)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var type = dateHistAgg.GetType();
            
            // 获取Buckets属性
            var bucketsProperty = type.GetProperty("Buckets");
            if (bucketsProperty != null)
            {
                var buckets = bucketsProperty.GetValue(dateHistAgg);
                if (buckets is IEnumerable<object> bucketList)
                {
                    var bucketResults = new List<Dictionary<string, object>>();
                    
                    foreach (var bucket in bucketList)
                    {
                        var bucketData = ParseBucketDynamic(bucket);
                        bucketResults.Add(bucketData);
                    }
                    
                    result["buckets"] = bucketResults;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析DateHistogram聚合失败");
            return new Dictionary<string, object> { ["error"] = "解析失败" };
        }
    }
    
    /// <summary>
    /// 动态解析值聚合结果
    /// </summary>
    private object ParseValueAggregationDynamic(object valueAgg)
    {
        try
        {
            var type = valueAgg.GetType();
            var valueProperty = type.GetProperty("Value");
            
            if (valueProperty != null)
            {
                var value = valueProperty.GetValue(valueAgg);
                return new Dictionary<string, object> { ["value"] = value ?? 0 };
            }
            
            return new Dictionary<string, object> { ["value"] = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析Value聚合失败");
            return new Dictionary<string, object> { ["value"] = 0 };
        }
    }
    
    /// <summary>
    /// 动态解析统计聚合结果
    /// </summary>
    private object ParseStatsAggregationDynamic(object statsAgg)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var type = statsAgg.GetType();
            
            // 获取统计属性
            var properties = new[] { "Count", "Min", "Max", "Avg", "Sum" };
            
            foreach (var propName in properties)
            {
                var property = type.GetProperty(propName);
                if (property != null)
                {
                    var value = property.GetValue(statsAgg);
                    result[propName.ToLowerInvariant()] = value ?? 0;
                }
            }
            
            // 检查是否是扩展统计
            var extendedProperties = new[] { "SumOfSquares", "Variance", "VariancePopulation", 
                "VarianceSampling", "StdDeviation", "StdDeviationPopulation", "StdDeviationSampling" };
            
            foreach (var propName in extendedProperties)
            {
                var property = type.GetProperty(propName);
                if (property != null)
                {
                    var value = property.GetValue(statsAgg);
                    result[ConvertPropertyNameToSnakeCase(propName)] = value ?? 0;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析Stats聚合失败");
            return new Dictionary<string, object> { ["error"] = "解析失败" };
        }
    }
    
    /// <summary>
    /// 动态解析范围聚合结果
    /// </summary>
    private object ParseRangeAggregationDynamic(object rangeAgg)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var type = rangeAgg.GetType();
            
            // 获取Buckets属性
            var bucketsProperty = type.GetProperty("Buckets");
            if (bucketsProperty != null)
            {
                var buckets = bucketsProperty.GetValue(rangeAgg);
                if (buckets is IEnumerable<object> bucketList)
                {
                    var bucketResults = new List<Dictionary<string, object>>();
                    
                    foreach (var bucket in bucketList)
                    {
                        var bucketData = ParseBucketDynamic(bucket);
                        
                        // 添加范围特有的属性
                        var bucketType = bucket.GetType();
                        var fromProperty = bucketType.GetProperty("From");
                        var toProperty = bucketType.GetProperty("To");
                        
                        if (fromProperty != null)
                        {
                            bucketData["from"] = fromProperty.GetValue(bucket);
                        }
                        
                        if (toProperty != null)
                        {
                            bucketData["to"] = toProperty.GetValue(bucket);
                        }
                        
                        bucketResults.Add(bucketData);
                    }
                    
                    result["buckets"] = bucketResults;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析Range聚合失败");
            return new Dictionary<string, object> { ["error"] = "解析失败" };
        }
    }
    
    /// <summary>
    /// 动态解析通用聚合结果
    /// </summary>
    private object ParseGenericAggregationDynamic(object aggregationValue)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var type = aggregationValue.GetType();
            
            // 获取所有公共属性
            var properties = type.GetProperties();
            
            foreach (var property in properties)
            {
                try
                {
                    var value = property.GetValue(aggregationValue);
                    if (value != null)
                    {
                        result[ConvertPropertyNameToSnakeCase(property.Name)] = value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取属性 {PropertyName} 失败", property.Name);
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态解析通用聚合失败");
            return aggregationValue;
        }
    }
    
    /// <summary>
    /// 动态解析Bucket
    /// </summary>
    private Dictionary<string, object> ParseBucketDynamic(object bucket)
    {
        var bucketData = new Dictionary<string, object>();
        var bucketType = bucket.GetType();
        
        // 获取基本属性
        var keyProperty = bucketType.GetProperty("Key");
        if (keyProperty != null)
        {
            bucketData["key"] = keyProperty.GetValue(bucket)?.ToString() ?? "";
        }
        
        var keyAsStringProperty = bucketType.GetProperty("KeyAsString");
        if (keyAsStringProperty != null)
        {
            bucketData["key_as_string"] = keyAsStringProperty.GetValue(bucket)?.ToString() ?? "";
        }
        
        var docCountProperty = bucketType.GetProperty("DocCount");
        if (docCountProperty != null)
        {
            bucketData["doc_count"] = docCountProperty.GetValue(bucket) ?? 0;
        }
        
        // 处理子聚合
        var aggregationsProperty = bucketType.GetProperty("Aggregations");
        if (aggregationsProperty != null)
        {
            var subAggregations = aggregationsProperty.GetValue(bucket);
            if (subAggregations != null && subAggregations is IDictionary<string, object> subAggDict)
            {
                foreach (var subAgg in subAggDict)
                {
                    bucketData[subAgg.Key] = ParseAggregationResult(subAgg.Key, subAgg.Value);
                }
            }
        }
        
        return bucketData;
    }
    
    /// <summary>
    /// 将属性名称转换为snake_case格式
    /// </summary>
    private string ConvertPropertyNameToSnakeCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;
            
        var result = new StringBuilder();
        
        for (int i = 0; i < propertyName.Length; i++)
        {
            char c = propertyName[i];
            
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('_');
            }
            
            result.Append(char.ToLowerInvariant(c));
        }
        
        return result.ToString();
    }
} 