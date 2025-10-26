using Xunit;
using Xunit.Abstractions;
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
    // 测试输出帮助器
    private readonly ITestOutputHelper _output;
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
    public AggregatorTests(ITestOutputHelper output)
    {
        _output = output;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _loggerMock = new Mock<ILogger<JsonNetAggregatorService>>();
        _httpContextMock = new Mock<HttpContext>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // 设置日志记录器以将日志输出到测试输出
        _loggerMock
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)))
            .Callback(new InvocationAction(invocation =>
            {
                var logLevel = (LogLevel)invocation.Arguments[0];
                var state = invocation.Arguments[2];
                var exception = (Exception)invocation.Arguments[3];
                var formatter = invocation.Arguments[4] as Delegate;
                var formattedMessage = formatter.DynamicInvoke(state, exception);
                _output.WriteLine($"{logLevel}: {formattedMessage}");
            }));

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

    /// <summary>
    /// 测试数据源路径中包含点号的情况
    /// 验证当URL中包含点号时是否能正确解析
    /// </summary>
    [Theory]
    [InlineData("/api.test/user/{value}.name", "api.test/user/123", "name")]
    [InlineData("/api.v1.test/user/{value}.data.name", "api.v1.test/user/123", "data.name")]
    [InlineData("/http://api.example.com/v1.0/user/{value}.profile.name", "http://api.example.com/v1.0/user/123", "profile.name")]
    public async Task DataSourceWithDots_ShouldParseCorrectly(string dataSource, string expectedPath, string expectedField)
    {
        // Arrange
        var input = new { userId = "123" };
        var aggregateKeys = $"userId={dataSource}";
        var mockResponse = new { name = "Test User" };

        _output.WriteLine($"Input: {JsonSerializer.Serialize(input)}");
        _output.WriteLine($"Aggregate Keys: {aggregateKeys}");
        _output.WriteLine($"Expected Path: {expectedPath}");
        _output.WriteLine($"Expected Field: {expectedField}");

        // 设置日志记录器以捕获日志
        var loggerSetup = new Mock<ILogger<JsonNetAggregatorService>>();
        var logMessages = new List<string>();

        loggerSetup
            .Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)))
            .Callback(new InvocationAction(invocation =>
            {
                var logLevel = (LogLevel)invocation.Arguments[0];
                var state = invocation.Arguments[2];
                var exception = (Exception)invocation.Arguments[3];
                var formatter = invocation.Arguments[4] as Delegate;
                var formattedMessage = formatter.DynamicInvoke(state, exception);
                logMessages.Add($"{logLevel}: {formattedMessage}");
                _output.WriteLine($"Log: {logLevel}: {formattedMessage}");
            }));

        var aggregatorService = new JsonNetAggregatorService(loggerSetup.Object);

        // 设置 HttpClientFactory
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        _httpClientFactoryMock.Setup(x => x.CreateClient("AggregationClient"))
            .Returns(() => httpClientFactory.CreateClient("AggregationClient"));

        var requestServices = new Mock<IServiceProvider>();
        requestServices.Setup(x => x.GetService(typeof(IHttpClientFactory)))
            .Returns(_httpClientFactoryMock.Object);
        _httpContextMock.Setup(x => x.RequestServices).Returns(requestServices.Object);

        // 设置 HTTP 请求处理
        var actualRequestUrl = string.Empty;
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
            {
                actualRequestUrl = request.RequestUri.ToString();
                _output.WriteLine($"Actual request URL: {actualRequestUrl}");
                _output.WriteLine($"Expected path: {expectedPath}");
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await aggregatorService.AggregateJsonContent(
            jsonContent, 
            new Dictionary<string, string> { { "userId", aggregateKeys } }, 
            _httpContextMock.Object
        );

        // 输出日志信息
        _output.WriteLine("\nLog messages:");
        foreach (var message in logMessages)
        {
            _output.WriteLine(message);
        }

        _output.WriteLine($"\nResult: {result}");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(actualRequestUrl);
        Assert.EndsWith(expectedPath, actualRequestUrl, StringComparison.OrdinalIgnoreCase);

        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
        Assert.Equal("Test User", resultObj.GetProperty("userId").GetString());
    }

    /// <summary>
    /// 测试无效数据源格式的处理
    /// 验证当数据源格式无效时是否能正确处理错误
    /// </summary>
    [Theory]
    [InlineData("invalid_path")]
    [InlineData("invalid_path.")]
    [InlineData(".invalid_field")]
    [InlineData("/path/without/field")]
    public async Task InvalidDataSource_ShouldReturnOriginalValue(string dataSource)
    {
        // Arrange
        var input = new { userId = "123" };
        var aggregateKeys = $"userId={dataSource}";
        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(
            jsonContent, 
            new Dictionary<string, string> { { "userId", aggregateKeys } }, 
            _httpContextMock.Object
        );
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("123", resultObj.GetProperty("userId").GetString());
    }

    /// <summary>
    /// 测试HTTP请求失败的情况
    /// 验证当HTTP请求失败时是否能正确处理错误并返回原始值
    /// </summary>
    [Theory]
    [InlineData(System.Net.HttpStatusCode.NotFound)]
    [InlineData(System.Net.HttpStatusCode.BadRequest)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError)]
    public async Task HttpRequestFailure_ShouldReturnOriginalValue(System.Net.HttpStatusCode statusCode)
    {
        // Arrange
        var input = new { userId = "123" };
        var aggregateKeys = "userId=/user/{value}.name";
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(new { error = "Error occurred" }))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(
            jsonContent, 
            new Dictionary<string, string> { { "userId", aggregateKeys } }, 
            _httpContextMock.Object
        );
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        Assert.Equal("123", resultObj.GetProperty("userId").GetString());
    }

    /// <summary>
    /// 测试请求头部传递
    /// 验证是否正确传递了验证头部信息
    /// </summary>
    [Fact]
    public async Task AuthorizationHeaders_ShouldBeProperlyPropagated()
    {
        // Arrange
        var input = new { userId = "123" };
        var aggregateKeys = "userId=/user/{value}.name";
        var authToken = "Bearer test-token";
        var apiKey = "test-api-key";

        var headers = new HeaderDictionary();
        headers.Add("Authorization", authToken);
        headers.Add("X-API-Key", apiKey);

        _httpContextMock.Setup(x => x.Request.Headers).Returns(headers);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Authorization.ToString() == authToken &&
                    req.Headers.Contains("X-API-Key") &&
                    req.Headers.GetValues("X-API-Key").First() == apiKey),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new { name = "Test User" }))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        await _aggregatorService.AggregateJsonContent(
            jsonContent, 
            new Dictionary<string, string> { { "userId", aggregateKeys } }, 
            _httpContextMock.Object
        );

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Headers.Authorization.ToString() == authToken &&
                req.Headers.Contains("X-API-Key") &&
                req.Headers.GetValues("X-API-Key").First() == apiKey),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    /// <summary>
    /// 测试复杂嵌套JSON响应的聚合处理
    /// 验证当输入为包含嵌套数组的复杂JSON时，是否能正确处理数组中对象的属性聚合
    /// </summary>
    [Fact]
    public async Task ComplexNestedJson_ShouldAggregateArrayItems()
    {
        // Arrange
        var input = new
        {
            status = 0,
            msg = "操作成功！",
            data = new
            {
                items = new[]
                {
                    new
                    {
                        id = 1,
                        appId = "identity",
                        appName = "用户中心",
                        environment = "Staging",
                        description = "批量发布配置",
                        version = "1",
                        createdAt = "2025-03-04 21:55:05",
                        createdBy = "1896921557544128512"
                    }
                },
                total = 1
            }
        };

        var aggregateKeys = "data.items.createdBy=/user/{value}.data.name#User-{value}";
        var mockResponse = new 
        { 
            data = new 
            { 
                name = "1896921557544128512" 
            }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString() == "http://test-api.example.com/user/1896921557544128512"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var jsonContent = JsonSerializer.Serialize(input);

        // Act
        var result = await _aggregatorService.AggregateJsonContent(jsonContent, new Dictionary<string, string> { { "data.items.createdBy", aggregateKeys } }, _httpContextMock.Object);
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result);

        // Assert
        var items = resultObj.GetProperty("data").GetProperty("items").EnumerateArray();
        var firstItem = items.First();
        Assert.Equal("User-1896921557544128512", firstItem.GetProperty("createdBy").GetString());
        Assert.Equal(0, resultObj.GetProperty("status").GetInt32());
        Assert.Equal("操作成功！", resultObj.GetProperty("msg").GetString());
        Assert.Equal(1, resultObj.GetProperty("data").GetProperty("total").GetInt32());
    }
}