using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Tests.Services;

/// <summary>
/// Elasticsearch服务索引前缀功能测试
/// </summary>
public class ElasticsearchServiceIndexPrefixTests
{
    private readonly ILogger<ElasticsearchService> _logger;
    
    public ElasticsearchServiceIndexPrefixTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ElasticsearchService>();
    }
    
    /// <summary>
    /// 测试无前缀时索引名称保持原样
    /// </summary>
    [Fact]
    public void GetFinalIndexName_WithoutPrefix_ShouldReturnOriginalIndexName()
    {
        // Arrange
        var configuration = CreateConfiguration("", "auditlogs");
        var service = new TestableElasticsearchService(_logger, configuration);
        
        // Act
        var indexName = service.GetFinalIndexNamePublic();
        
        // Assert
        Assert.Equal("auditlogs", indexName);
    }
    
    /// <summary>
    /// 测试有前缀时索引名称格式正确
    /// </summary>
    [Fact]
    public void GetFinalIndexName_WithPrefix_ShouldReturnPrefixedIndexName()
    {
        // Arrange
        var configuration = CreateConfiguration("dev", "auditlogs");
        var service = new TestableElasticsearchService(_logger, configuration);
        
        // Act
        var indexName = service.GetFinalIndexNamePublic();
        
        // Assert
        Assert.Equal("dev_auditlogs", indexName);
    }
    
    /// <summary>
    /// 测试前缀为空白字符时的处理
    /// </summary>
    [Fact]
    public void GetFinalIndexName_WithWhitespacePrefix_ShouldReturnOriginalIndexName()
    {
        // Arrange
        var configuration = CreateConfiguration("   ", "auditlogs");
        var service = new TestableElasticsearchService(_logger, configuration);
        
        // Act
        var indexName = service.GetFinalIndexNamePublic();
        
        // Assert
        Assert.Equal("auditlogs", indexName);
    }
    
    /// <summary>
    /// 测试不同环境前缀的正确处理
    /// </summary>
    [Theory]
    [InlineData("dev", "auditlogs", "dev_auditlogs")]
    [InlineData("test", "auditlogs", "test_auditlogs")]
    [InlineData("prod", "auditlogs", "prod_auditlogs")]
    [InlineData("staging", "user-audit", "staging_user-audit")]
    public void GetFinalIndexName_WithVariousPrefixes_ShouldReturnCorrectFormat(
        string prefix, string indexName, string expected)
    {
        // Arrange
        var configuration = CreateConfiguration(prefix, indexName);
        var service = new TestableElasticsearchService(_logger, configuration);
        
        // Act
        var result = service.GetFinalIndexNamePublic();
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    /// <summary>
    /// 创建测试配置
    /// </summary>
    /// <param name="indexPrefix">索引前缀</param>
    /// <param name="indexName">索引名称</param>
    /// <returns>配置对象</returns>
    private static IConfiguration CreateConfiguration(string indexPrefix, string indexName)
    {
        var configData = new Dictionary<string, string>
        {
            ["Audit:Elasticsearch:IndexPrefix"] = indexPrefix,
            ["Audit:Elasticsearch:IndexName"] = indexName,
            ["Audit:Elasticsearch:Urls:0"] = "http://localhost:9200",
            ["Audit:Elasticsearch:UserName"] = "",
            ["Audit:Elasticsearch:Password"] = "",
            ["Audit:Elasticsearch:NumberOfShards"] = "1",
            ["Audit:Elasticsearch:NumberOfReplicas"] = "1"
        };
        
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }
}

/// <summary>
/// 可测试的Elasticsearch服务实现，暴露私有方法用于测试
/// </summary>
public class TestableElasticsearchService : ElasticsearchService
{
    public TestableElasticsearchService(
        ILogger<ElasticsearchService> logger, 
        IConfiguration configuration) 
        : base(logger, configuration, null)
    {
    }
    
    /// <summary>
    /// 公开GetFinalIndexName方法用于测试
    /// </summary>
    /// <returns>最终索引名称</returns>
    public string GetFinalIndexNamePublic()
    {
        // 使用反射调用私有方法
        var method = typeof(ElasticsearchService).GetMethod("GetFinalIndexName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (string)method!.Invoke(this, null)!;
    }
} 