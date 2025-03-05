using Xunit;
using System.Text.Json;
using System.Net.Http;
using Moq;
using Moq.Protected;
using CodeSpirit.Aggregator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Aggregator.Tests;

/// <summary>
/// JsonNet聚合器服务的单元测试类
/// 测试JSON数据的静态替换、动态替换和动态补充功能
/// </summary>
public class AggregatorTests
{
    // JSON序列化选项，设置属性名称大小写不敏感
    private readonly JsonSerializerOptions _jsonOptions;
    // 被测试的聚合器服务实例
    private readonly JsonNetAggregatorService _aggregatorService;
    // 日志服务的模拟对象
    private readonly Mock<ILogger<JsonNetAggregatorService>> _loggerMock;
    // HTTP上下文的模拟对象
    private readonly Mock<HttpContext> _httpContextMock;
    // HTTP客户端工厂的模拟对象
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    // HTTP消息处理器的模拟对象
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    // 服务提供者实例
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 测试类构造函数，初始化所有必要的模拟对象和服务
    /// </summary>
    public AggregatorTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _loggerMock = new Mock<ILogger<JsonNetAggregatorService>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        var services = new ServiceCollection();
        services.AddHttpClient("AggregationClient", client =>
        {
            client.BaseAddress = new Uri("http://test-api.example.com");
        })
        .ConfigurePrimaryHttpMessageHandler(() => _httpMessageHandlerMock.Object);
        _serviceProvider = services.BuildServiceProvider();

        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient("AggregationClient"))
            .Returns(() => httpClientFactory.CreateClient("AggregationClient"));

        // Configure HttpContext mock with all necessary properties
        var requestServices = new Mock<IServiceProvider>();
        requestServices.Setup(x => x.GetService(typeof(IHttpClientFactory)))
            .Returns(_httpClientFactoryMock.Object);

        _httpContextMock.Setup(x => x.RequestServices).Returns(requestServices.Object);
        _httpContextMock.Setup(x => x.Request).Returns(new Mock<HttpRequest>().Object);
        _httpContextMock.Setup(x => x.Response).Returns(new Mock<HttpResponse>().Object);

        // Configure the HTTP message handler to return a properly structured response
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var content = new StringContent("{}");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                var response = new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = content,
                    Version = new Version(1, 1),
                    ReasonPhrase = "OK",
                    RequestMessage = request
                };
                return response;
            });

        _aggregatorService = new JsonNetAggregatorService(_loggerMock.Object);
    }

    /// <summary>
    /// 测试静态替换功能
    /// 验证当使用模板格式时，是否正确替换值
    /// </summary>
    [Fact]
    public async Task StaticReplacement_ShouldApplyTemplate()
    {
        // Arrange
        var input = new
        {
            id = 123,
            createdBy = "10001"
        };

        var aggregateKeys = "createdBy#User-{value}";
        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "createdBy", aggregateKeys } }, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("User-10001", resultObj.GetProperty("createdBy").GetString());
    }

    /// <summary>
    /// 测试动态替换功能
    /// 验证当需要从外部API获取数据时，是否正确获取并替换值
    /// </summary>
    [Fact]
    public async Task DynamicReplacement_ShouldFetchAndReplaceValue()
    {
        // Arrange
        var input = new
        {
            id = 123,
            updatedBy = "10002"
        };

        var aggregateKeys = "updatedBy=/user/{value}.name";
        var mockResponse = new { name = "User-10002" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == "http://test-api.example.com/user/10002"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "updatedBy", aggregateKeys } }, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("User-10002", resultObj.GetProperty("updatedBy").GetString());
    }

    /// <summary>
    /// 测试动态补充功能
    /// 验证当需要从外部API获取数据并追加到原值时，是否正确处理
    /// </summary>
    [Fact]
    public async Task DynamicSupplementation_ShouldAppendDataSourceValue()
    {
        // Arrange
        var input = new
        {
            id = 123,
            items = new[]
            {
                new { itemId = 1, createdBy = "10003" }
            }
        };

        var aggregateKeys = "items.createdBy=/user/{value}.fullName#{value} ({field})";
        var mockResponse = new { fullName = "User-10003" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == "http://test-api.example.com/user/10003"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "items.createdBy", aggregateKeys } }, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        var items = resultObj.GetProperty("items").EnumerateArray();
        var firstItem = items.First();
        Assert.Equal("10003 (User-10003)", firstItem.GetProperty("createdBy").GetString());
    }

    /// <summary>
    /// 测试多重聚合功能
    /// 验证当同时存在多种聚合规则时，是否都能正确处理
    /// </summary>
    [Fact]
    public async Task MultipleAggregations_ShouldHandleAllRules()
    {
        // Arrange
        var input = new
        {
            id = 123,
            title = "测试文档",
            createdBy = "10001",
            updatedBy = "10002",
            items = new[]
            {
                new { itemId = 1, createdBy = "10003" }
            }
        };

        var aggregateKeys = new Dictionary<string, string>
        {
            { "createdBy", "createdBy#User-{value}" },
            { "updatedBy", "updatedBy=/user/{value}.name" },
            { "items.createdBy", "items.createdBy=/user/{value}.fullName#{value} ({field})" }
        };

        var mockResponse = new { name = "User-10002", fullName = "User-10003" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && 
                    (req.RequestUri.ToString() == "http://test-api.example.com/user/10002" || 
                     req.RequestUri.ToString() == "http://test-api.example.com/user/10003")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize<object>(
                    req.RequestUri.ToString() == "http://test-api.example.com/user/10002" 
                        ? new { name = "User-10002" }
                        : new { fullName = "User-10003" }
                ))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, aggregateKeys, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("User-10001", resultObj.GetProperty("createdBy").GetString());
        Assert.Equal("User-10002", resultObj.GetProperty("updatedBy").GetString());
        var items = resultObj.GetProperty("items").EnumerateArray();
        var firstItem = items.First();
        Assert.Equal("10003 (User-10003)", firstItem.GetProperty("createdBy").GetString());
    }

    /// <summary>
    /// 测试嵌套属性路径
    /// 验证当响应数据包含多层级的属性路径时，是否能正确获取和替换值
    /// </summary>
    [Fact]
    public async Task NestedPropertyPath_ShouldHandleCorrectly()
    {
        // Arrange
        var input = new
        {
            id = 123,
            userId = "10004"
        };

        var aggregateKeys = "userId=/user/{value}.data.name#User-{value}";
        var mockResponse = new 
        { 
            data = new 
            { 
                name = "10004" 
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString() == "http://test-api.example.com/user/10004"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);
        Console.WriteLine($"Input JSON: {jsonContent}");
        Console.WriteLine($"Mock Response: {JsonSerializer.Serialize(mockResponse)}");

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "userId", aggregateKeys } }, _httpContextMock.Object);
        Console.WriteLine($"Result JSON: {result}");
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("User-10004", resultObj.GetProperty("userId").GetString());
    }

    /// <summary>
    /// 测试长整型ID的聚合处理
    /// 验证当输入为long类型的UserId时，是否能正确聚合为字符串类型
    /// </summary>
    [Fact]
    public async Task LongTypeUserId_ShouldAggregateToString()
    {
        // Arrange
        var input = new
        {
            id = 123,
            userId = 100045678L // 使用long类型的UserId
        };

        var aggregateKeys = "userId=/user/{value}.data.name#User-{value}";
        var mockResponse = new 
        { 
            data = new 
            { 
                name = "100045678" 
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString() == "http://test-api.example.com/user/100045678"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "userId", aggregateKeys } }, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("User-100045678", resultObj.GetProperty("userId").GetString());
    }
}