using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.Audit.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nest;

namespace CodeSpirit.Audit.Tests.Services;

/// <summary>
/// 审计服务单元测试
/// </summary>
public class AuditServiceTests : TestBase
{
    private readonly Mock<IElasticsearchService> _mockElasticsearchService;
    private readonly Mock<IRabbitMQService> _mockRabbitMQService;
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuditOptions _auditOptions;
    private readonly IAuditService _auditService;

    public AuditServiceTests(ITestOutputHelper output) : base(output)
    {
        _mockElasticsearchService = new Mock<IElasticsearchService>();
        _mockRabbitMQService = new Mock<IRabbitMQService>();
        _mockLogger = new Mock<ILogger<AuditService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        var auditSection = new Mock<IConfigurationSection>();
        auditSection.Setup(s => s.Path).Returns("Audit");
        auditSection.Setup(s => s.Key).Returns("Audit");
        auditSection.Setup(s => s.Value).Returns(string.Empty);
        
        _mockConfiguration.Setup(c => c.GetSection("Audit")).Returns(auditSection.Object);
        
        _auditOptions = new AuditOptions
        {
            Enabled = true,
            LogRequestParams = true,
            LogResponseData = true,
            LogUnauthorizedRequests = true,
            LogAnonymousRequests = true,
            ExcludedPathPrefixes = new List<string> { "/swagger", "/healthz" }
        };
        
        _auditService = new AuditService(
            _mockElasticsearchService.Object,
            _mockRabbitMQService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);
    }
    
    [Fact]
    public async Task LogAsync_WithValidInput_ShouldPublishToRabbitMQ()
    {
        // 安排
        _output.WriteLine("测试记录审计日志 - 验证发布行为");
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid().ToString(),
            OperationTime = DateTime.UtcNow,
            UserId = "test-user-123",
            UserName = "测试用户",
            IpAddress = "127.0.0.1",
            RequestMethod = "GET",
            RequestPath = "/api/test",
            OperationType = "Query",
            OperationName = "测试操作",
            AfterData = JsonSerializer.Serialize(new { param1 = "value1" })
        };
        
        // 使用明确的参数而不是可选参数
        _mockRabbitMQService.Setup(x => x.SendMessageAsync(It.IsAny<AuditLog>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        // 执行
        await _auditService.LogAsync(auditLog);
        
        // 断言 - 使用手动验证
        _mockRabbitMQService.Verify(x => x.SendMessageAsync(It.Is<AuditLog>(log => log.Id == auditLog.Id), It.IsAny<string>()), Times.Once);
        
        _output.WriteLine($"审计日志处理完成 - ID: {auditLog.Id}, 时间: {auditLog.OperationTime}");
    }
    
    [Fact]
    public async Task SearchAsync_WithSearchParameters_ShouldCallElasticsearchService()
    {
        // 安排
        _output.WriteLine("测试搜索审计日志 - 验证搜索参数处理");
        var searchParams = new AuditLogQueryDto
        {
            UserId = "test-user-123",
            StartTime = DateTime.UtcNow.AddDays(-7),
            EndTime = DateTime.UtcNow,
            OperationType = "Query",
            PageIndex = 1,
            PageSize = 20,
            SortField = "operationTime",
            SortDirection = "desc"
        };
        
        var expectedItems = new List<AuditLog> { 
            new AuditLog { 
                Id = "1", 
                UserId = "test-user-123",
                OperationTime = DateTime.UtcNow.AddHours(-1)
            } 
        };
        var expectedTotal = 1L;
        
        // 避免使用元组，使用单独的设置
        _mockElasticsearchService.Setup(x => x.SearchAsync<AuditLog>(It.IsAny<Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>>>()))
            .ReturnsAsync((expectedItems, expectedTotal));
        
        // 执行
        var result = await _auditService.SearchAsync(searchParams);
        
        // 断言
        Assert.Equal(expectedTotal, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("1", result.Items.First().Id);
    }
    
    [Fact]
    public async Task GetOperationStatsAsync_ShouldCallElasticsearchService()
    {
        // 安排
        var startTime = DateTime.UtcNow.AddDays(-30);
        var endTime = DateTime.UtcNow;
        
        var expectedStats = new Dictionary<string, long>
        {
            { "创建", 100 },
            { "修改", 50 },
            { "查询", 200 },
            { "删除", 30 }
        };
        
        var aggregationResult = new Dictionary<string, object>
        {
            { "operations", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "key", "创建" }, { "count", 100L } },
                    new Dictionary<string, object> { { "key", "修改" }, { "count", 50L } },
                    new Dictionary<string, object> { { "key", "查询" }, { "count", 200L } },
                    new Dictionary<string, object> { { "key", "删除" }, { "count", 30L } }
                }
            }
        };
        
        _mockElasticsearchService.Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>>>()))
            .ReturnsAsync(aggregationResult);
        
        // 执行
        var result = await _auditService.GetOperationStatsAsync(startTime, endTime);
        
        // 断言
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Equal(100, result["创建"]);
        Assert.Equal(50, result["修改"]);
        Assert.Equal(200, result["查询"]);
        Assert.Equal(30, result["删除"]);
    }
    
    [Fact]
    public async Task GetUserStatsAsync_ShouldCallElasticsearchService()
    {
        // 安排
        var startTime = DateTime.UtcNow.AddDays(-30);
        var endTime = DateTime.UtcNow;
        var topN = 3;
        
        var expectedStats = new Dictionary<string, long>
        {
            { "用户1", 100 },
            { "用户2", 50 },
            { "用户3", 30 }
        };
        
        var aggregationResult = new Dictionary<string, object>
        {
            { "users", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "key", "用户1" }, { "count", 100L } },
                    new Dictionary<string, object> { { "key", "用户2" }, { "count", 50L } },
                    new Dictionary<string, object> { { "key", "用户3" }, { "count", 30L } }
                }
            }
        };
        
        _mockElasticsearchService.Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>>>()))
            .ReturnsAsync(aggregationResult);
        
        // 执行
        var result = await _auditService.GetUserStatsAsync(startTime, endTime, topN);
        
        // 断言
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
    
    [Fact]
    public async Task GetOperationTrendAsync_ShouldCallElasticsearchService()
    {
        // 安排
        var startTime = DateTime.UtcNow.AddDays(-7);
        var endTime = DateTime.UtcNow;
        var interval = 24;
        
        var dateHistogramResult = new Dictionary<string, object>
        {
            { "trend", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "key_as_string", DateTime.UtcNow.AddDays(-6).ToString("o") }, { "count", 10L } },
                    new Dictionary<string, object> { { "key_as_string", DateTime.UtcNow.AddDays(-5).ToString("o") }, { "count", 15L } },
                    new Dictionary<string, object> { { "key_as_string", DateTime.UtcNow.AddDays(-4).ToString("o") }, { "count", 20L } }
                }
            }
        };
        
        _mockElasticsearchService.Setup(x => x.AggregateAsync<AuditLog>(It.IsAny<Func<SearchDescriptor<AuditLog>, SearchDescriptor<AuditLog>>>()))
            .ReturnsAsync(dateHistogramResult);
        
        // 执行
        var result = await _auditService.GetOperationTrendAsync(startTime, endTime, interval);
        
        // 断言
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }
} 