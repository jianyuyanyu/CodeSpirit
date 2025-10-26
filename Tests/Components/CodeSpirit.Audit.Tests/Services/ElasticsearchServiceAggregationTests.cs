using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace CodeSpirit.Audit.Tests.Services;

/// <summary>
/// ElasticsearchService 聚合查询功能测试
/// </summary>
public class ElasticsearchServiceAggregationTests
{
    private readonly Mock<ILogger<ElasticsearchService>> _mockLogger;
    private readonly IConfiguration _configuration;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ElasticsearchServiceAggregationTests()
    {
        _mockLogger = new Mock<ILogger<ElasticsearchService>>();
        
        // 创建测试配置
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:IndexName"] = "test-audit-logs",
            ["Audit:Elasticsearch:IndexPrefix"] = "test",
            ["Audit:Elasticsearch:NumberOfShards"] = "1",
            ["Audit:Elasticsearch:NumberOfReplicas"] = "0"
        });
        _configuration = configBuilder.Build();
    }
    
    /// <summary>
    /// 测试属性名称转换为snake_case
    /// </summary>
    [Theory]
    [InlineData("OperationType", "operation_type")]
    [InlineData("UserId", "user_id")]
    [InlineData("DocCount", "doc_count")]
    [InlineData("SumOtherDocCount", "sum_other_doc_count")]
    [InlineData("StdDeviationPopulation", "std_deviation_population")]
    public void ConvertPropertyNameToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ConvertPropertyNameToSnakeCase", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(service, new object[] { input }) as string;
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    /// <summary>
    /// 测试解析Terms聚合结果
    /// </summary>
    [Fact]
    public void ParseTermsAggregationDynamic_ShouldParseCorrectly()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseTermsAggregationDynamic", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 创建模拟的Terms聚合对象
        var mockTermsAgg = CreateMockTermsAggregation();
        
        // Act
        var result = method?.Invoke(service, new object[] { mockTermsAgg }) as Dictionary<string, object>;
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("buckets"));
        Assert.True(result.ContainsKey("doc_count_error_upper_bound"));
        Assert.True(result.ContainsKey("sum_other_doc_count"));
        
        var buckets = result["buckets"] as List<Dictionary<string, object>>;
        Assert.NotNull(buckets);
        Assert.NotEmpty(buckets);
    }
    
    /// <summary>
    /// 测试解析Value聚合结果
    /// </summary>
    [Fact]
    public void ParseValueAggregationDynamic_ShouldParseCorrectly()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseValueAggregationDynamic", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 创建模拟的Value聚合对象
        var mockValueAgg = CreateMockValueAggregation(123.45);
        
        // Act
        var result = method?.Invoke(service, new object[] { mockValueAgg }) as Dictionary<string, object>;
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("value"));
        Assert.Equal(123.45, result["value"]);
    }
    
    /// <summary>
    /// 测试解析Stats聚合结果
    /// </summary>
    [Fact]
    public void ParseStatsAggregationDynamic_ShouldParseCorrectly()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseStatsAggregationDynamic", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 创建模拟的Stats聚合对象
        var mockStatsAgg = CreateMockStatsAggregation();
        
        // Act
        var result = method?.Invoke(service, new object[] { mockStatsAgg }) as Dictionary<string, object>;
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("count"));
        Assert.True(result.ContainsKey("min"));
        Assert.True(result.ContainsKey("max"));
        Assert.True(result.ContainsKey("avg"));
        Assert.True(result.ContainsKey("sum"));
        
        Assert.Equal(100L, result["count"]);
        Assert.Equal(1.0, result["min"]);
        Assert.Equal(100.0, result["max"]);
        Assert.Equal(50.5, result["avg"]);
        Assert.Equal(5050.0, result["sum"]);
    }
    
    /// <summary>
    /// 测试解析DateHistogram聚合结果
    /// </summary>
    [Fact]
    public void ParseDateHistogramAggregationDynamic_ShouldParseCorrectly()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseDateHistogramAggregationDynamic", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 创建模拟的DateHistogram聚合对象
        var mockDateHistAgg = CreateMockDateHistogramAggregation();
        
        // Act
        var result = method?.Invoke(service, new object[] { mockDateHistAgg }) as Dictionary<string, object>;
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("buckets"));
        
        var buckets = result["buckets"] as List<Dictionary<string, object>>;
        Assert.NotNull(buckets);
        Assert.NotEmpty(buckets);
    }
    
    /// <summary>
    /// 测试解析Bucket
    /// </summary>
    [Fact]
    public void ParseBucketDynamic_ShouldParseCorrectly()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseBucketDynamic", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 创建模拟的Bucket对象
        var mockBucket = CreateMockBucket("test_key", 42);
        
        // Act
        var result = method?.Invoke(service, new object[] { mockBucket }) as Dictionary<string, object>;
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("key"));
        Assert.True(result.ContainsKey("doc_count"));
        
        Assert.Equal("test_key", result["key"]);
        Assert.Equal(42L, result["doc_count"]);
    }
    
    /// <summary>
    /// 测试聚合结果解析异常处理
    /// </summary>
    [Fact]
    public void ParseAggregationResult_WithInvalidInput_ShouldHandleGracefully()
    {
        // Arrange
        var service = new ElasticsearchService(_mockLogger.Object, _configuration);
        var method = typeof(ElasticsearchService).GetMethod("ParseAggregationResult", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act & Assert - 测试null输入
        var result1 = method?.Invoke(service, new object[] { "test", null! });
        Assert.Null(result1);
        
        // Act & Assert - 测试无效对象
        var result2 = method?.Invoke(service, new object[] { "test", "invalid_object" });
        Assert.NotNull(result2);
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 创建模拟的Terms聚合对象
    /// </summary>
    private object CreateMockTermsAggregation()
    {
        var buckets = new List<object>
        {
            CreateMockBucket("CREATE", 10),
            CreateMockBucket("UPDATE", 5),
            CreateMockBucket("DELETE", 2)
        };
        
        return new
        {
            Buckets = buckets,
            DocCountErrorUpperBound = 0L,
            SumOtherDocCount = 1L
        };
    }
    
    /// <summary>
    /// 创建模拟的Value聚合对象
    /// </summary>
    private object CreateMockValueAggregation(double value)
    {
        return new { Value = value };
    }
    
    /// <summary>
    /// 创建模拟的Stats聚合对象
    /// </summary>
    private object CreateMockStatsAggregation()
    {
        return new
        {
            Count = 100L,
            Min = 1.0,
            Max = 100.0,
            Avg = 50.5,
            Sum = 5050.0
        };
    }
    
    /// <summary>
    /// 创建模拟的DateHistogram聚合对象
    /// </summary>
    private object CreateMockDateHistogramAggregation()
    {
        var buckets = new List<object>
        {
            CreateMockDateBucket(1640995200000L, 15), // 2022-01-01 00:00:00
            CreateMockDateBucket(1640998800000L, 20), // 2022-01-01 01:00:00
            CreateMockDateBucket(1641002400000L, 18)  // 2022-01-01 02:00:00
        };
        
        return new { Buckets = buckets };
    }
    
    /// <summary>
    /// 创建模拟的Bucket对象
    /// </summary>
    private object CreateMockBucket(string key, long docCount)
    {
        return new
        {
            Key = key,
            KeyAsString = key,
            DocCount = docCount,
            Aggregations = new Dictionary<string, object>()
        };
    }
    
    /// <summary>
    /// 创建模拟的日期Bucket对象
    /// </summary>
    private object CreateMockDateBucket(long timestamp, long docCount)
    {
        return new
        {
            Key = timestamp,
            KeyAsString = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            DocCount = docCount,
            Aggregations = new Dictionary<string, object>()
        };
    }
    
    #endregion
} 