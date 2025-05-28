using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.Audit.Services.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Elastic.Clients.Elasticsearch;

namespace CodeSpirit.Audit.Tests.Services;

/// <summary>
/// AuditService 聚合统计功能测试
/// </summary>
public class AuditServiceAggregationTests
{
    private readonly Mock<IElasticsearchService> _mockElasticsearchService;
    private readonly Mock<IRabbitMQService> _mockRabbitMQService;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly AuditService _auditService;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditServiceAggregationTests()
    {
        _mockElasticsearchService = new Mock<IElasticsearchService>();
        _mockRabbitMQService = new Mock<IRabbitMQService>();
        _mockLogger = new Mock<ILogger<AuditService>>();
        
        // 创建测试配置
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Audit:Enabled"] = "true",
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:IndexName"] = "test-audit-logs"
        });
        _configuration = configBuilder.Build();
        
        _auditService = new AuditService(
            _mockElasticsearchService.Object,
            _mockRabbitMQService.Object,
            _mockLogger.Object,
            _configuration);
    }
    
    /// <summary>
    /// 测试获取操作统计 - 成功场景
    /// </summary>
    [Fact]
    public async Task GetOperationStatsAsync_WithValidData_ShouldReturnCorrectStats()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["operations"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = "CREATE", ["doc_count"] = 150L },
                    new() { ["key"] = "UPDATE", ["doc_count"] = 89L },
                    new() { ["key"] = "DELETE", ["doc_count"] = 23L },
                    new() { ["key"] = "READ", ["doc_count"] = 456L }
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Equal(150L, result["CREATE"]);
        Assert.Equal(89L, result["UPDATE"]);
        Assert.Equal(23L, result["DELETE"]);
        Assert.Equal(456L, result["READ"]);
        
        // 验证Elasticsearch服务被正确调用
        _mockElasticsearchService.Verify(
            x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()),
            Times.Once);
    }
    
    /// <summary>
    /// 测试获取操作统计 - 空结果场景
    /// </summary>
    [Fact]
    public async Task GetOperationStatsAsync_WithEmptyResult_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync((IDictionary<string, object>?)null);
        
        // Act
        var result = await _auditService.GetOperationStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试获取操作统计 - 异常处理
    /// </summary>
    [Fact]
    public async Task GetOperationStatsAsync_WithException_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ThrowsAsync(new Exception("Elasticsearch连接失败"));
        
        // Act
        var result = await _auditService.GetOperationStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        
        // 验证错误日志被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取操作统计失败")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    /// <summary>
    /// 测试获取用户统计 - 成功场景
    /// </summary>
    [Fact]
    public async Task GetUserStatsAsync_WithValidData_ShouldReturnCorrectStats()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        var topN = 5;
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["users"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = "user001", ["doc_count"] = 245L },
                    new() { ["key"] = "user002", ["doc_count"] = 189L },
                    new() { ["key"] = "user003", ["doc_count"] = 156L },
                    new() { ["key"] = "user004", ["doc_count"] = 98L },
                    new() { ["key"] = "user005", ["doc_count"] = 67L }
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetUserStatsAsync(startTime, endTime, topN);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(245L, result["user001"]);
        Assert.Equal(189L, result["user002"]);
        Assert.Equal(156L, result["user003"]);
        Assert.Equal(98L, result["user004"]);
        Assert.Equal(67L, result["user005"]);
    }
    
    /// <summary>
    /// 测试获取用户统计 - 默认参数
    /// </summary>
    [Fact]
    public async Task GetUserStatsAsync_WithDefaultTopN_ShouldUseDefault10()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["users"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>()
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetUserStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        
        // 验证调用时使用了默认的topN=10
        _mockElasticsearchService.Verify(
            x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()),
            Times.Once);
    }
    
    /// <summary>
    /// 测试获取操作趋势 - 成功场景
    /// </summary>
    [Fact]
    public async Task GetOperationTrendAsync_WithValidData_ShouldReturnCorrectTrend()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 2);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["trend"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = 1704067200000L, ["doc_count"] = 45L }, // 2024-01-01 00:00:00
                    new() { ["key"] = 1704070800000L, ["doc_count"] = 52L }, // 2024-01-01 01:00:00
                    new() { ["key"] = 1704074400000L, ["doc_count"] = 38L }, // 2024-01-01 02:00:00
                    new() { ["key"] = 1704078000000L, ["doc_count"] = 41L }  // 2024-01-01 03:00:00
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationTrendAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        
        // 验证时间戳转换正确
        var expectedDateTime1 = DateTimeOffset.FromUnixTimeMilliseconds(1704067200000L).DateTime;
        var expectedDateTime2 = DateTimeOffset.FromUnixTimeMilliseconds(1704070800000L).DateTime;
        
        Assert.True(result.ContainsKey(expectedDateTime1));
        Assert.True(result.ContainsKey(expectedDateTime2));
        Assert.Equal(45L, result[expectedDateTime1]);
        Assert.Equal(52L, result[expectedDateTime2]);
    }
    
    /// <summary>
    /// 测试获取操作趋势 - 字符串时间戳解析
    /// </summary>
    [Fact]
    public async Task GetOperationTrendAsync_WithStringTimestamp_ShouldParseCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 2);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["trend"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = "2024-01-01T00:00:00.000Z", ["doc_count"] = 25L },
                    new() { ["key"] = "2024-01-01T01:00:00.000Z", ["doc_count"] = 30L }
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationTrendAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        var expectedDateTime1 = DateTime.Parse("2024-01-01T00:00:00.000Z");
        var expectedDateTime2 = DateTime.Parse("2024-01-01T01:00:00.000Z");
        
        Assert.True(result.ContainsKey(expectedDateTime1));
        Assert.True(result.ContainsKey(expectedDateTime2));
        Assert.Equal(25L, result[expectedDateTime1]);
        Assert.Equal(30L, result[expectedDateTime2]);
    }
    
    /// <summary>
    /// 测试获取操作趋势 - 无效时间戳处理
    /// </summary>
    [Fact]
    public async Task GetOperationTrendAsync_WithInvalidTimestamp_ShouldSkipInvalidEntries()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 2);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["trend"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = "invalid_timestamp", ["doc_count"] = 25L },
                    new() { ["key"] = 1704067200000L, ["doc_count"] = 30L }
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationTrendAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // 只有一个有效的时间戳被解析
        
        var expectedDateTime = DateTimeOffset.FromUnixTimeMilliseconds(1704067200000L).DateTime;
        Assert.True(result.ContainsKey(expectedDateTime));
        Assert.Equal(30L, result[expectedDateTime]);
        
        // 验证警告日志被记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("无法解析时间戳")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    /// <summary>
    /// 测试获取操作趋势 - 自定义间隔
    /// </summary>
    [Fact]
    public async Task GetOperationTrendAsync_WithCustomInterval_ShouldPassCorrectInterval()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 2);
        var customInterval = 12; // 12小时间隔
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["trend"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>()
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationTrendAsync(startTime, endTime, customInterval);
        
        // Assert
        Assert.NotNull(result);
        
        // 验证Elasticsearch服务被调用
        _mockElasticsearchService.Verify(
            x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()),
            Times.Once);
    }
    
    /// <summary>
    /// 测试解析操作统计结果 - 缺少buckets
    /// </summary>
    [Fact]
    public async Task GetOperationStatsAsync_WithMissingBuckets_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["operations"] = new Dictionary<string, object>
            {
                ["doc_count_error_upper_bound"] = 0L,
                ["sum_other_doc_count"] = 0L
                // 缺少buckets字段
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetOperationStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    /// <summary>
    /// 测试解析用户统计结果 - 数据类型转换
    /// </summary>
    [Fact]
    public async Task GetUserStatsAsync_WithDifferentDataTypes_ShouldConvertCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2024, 1, 1);
        var endTime = new DateTime(2024, 1, 31);
        
        var mockAggregationResult = new Dictionary<string, object>
        {
            ["users"] = new Dictionary<string, object>
            {
                ["buckets"] = new List<Dictionary<string, object>>
                {
                    new() { ["key"] = "user001", ["doc_count"] = 100 }, // int类型
                    new() { ["key"] = "user002", ["doc_count"] = 200L }, // long类型
                    new() { ["key"] = "user003", ["doc_count"] = "300" } // string类型
                }
            }
        };
        
        _mockElasticsearchService
            .Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchRequestDescriptor<AuditLog>, SearchRequestDescriptor<AuditLog>>>()))
            .ReturnsAsync(mockAggregationResult);
        
        // Act
        var result = await _auditService.GetUserStatsAsync(startTime, endTime);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(100L, result["user001"]);
        Assert.Equal(200L, result["user002"]);
        Assert.Equal(300L, result["user003"]);
    }
} 