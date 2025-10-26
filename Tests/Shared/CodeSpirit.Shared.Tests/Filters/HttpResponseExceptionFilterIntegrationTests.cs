using CodeSpirit.Core;
using CodeSpirit.Shared.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CodeSpirit.Shared.Tests.Filters;

/// <summary>
/// HttpResponseExceptionFilter 性能和边界测试
/// </summary>
public class HttpResponseExceptionFilterPerformanceTests
{
    private readonly Mock<ILogger<HttpResponseExceptionFilter>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly HttpResponseExceptionFilter _filter;

    public HttpResponseExceptionFilterPerformanceTests()
    {
        _mockLogger = new Mock<ILogger<HttpResponseExceptionFilter>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _filter = new HttpResponseExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
    }

    /// <summary>
    /// 创建异常上下文
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <returns>异常上下文</returns>
    private ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = Guid.NewGuid().ToString();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/performance/test";
        httpContext.Request.QueryString = new QueryString("?test=performance");
        httpContext.Request.Headers.UserAgent = "Performance-Test-Agent";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    [Fact]
    public void OnException_PerformanceTest_HandlesMultipleExceptionsQuickly()
    {
        // Arrange
        var exceptions = new List<Exception>
        {
            new BusinessException("业务异常1"),
            new ValidationException("验证异常1"),
            new ArgumentNullException("参数异常1"),
            new UnauthorizedAccessException("权限异常1"),
            new FileNotFoundException("文件异常1")
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        foreach (var exception in exceptions)
        {
            var context = CreateExceptionContext(exception);
            _filter.OnException(context);
            Assert.True(context.ExceptionHandled);
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"处理5个异常耗时 {stopwatch.ElapsedMilliseconds}ms，超过预期的100ms");
    }

    [Fact]
    public void OnException_LargeExceptionMessage_HandlesCorrectly()
    {
        // Arrange
        var largeMessage = new string('A', 10000); // 10KB 的错误消息
        var exception = new BusinessException(largeMessage);
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        var msgProperty = responseValue.GetType().GetProperty("msg");
        Assert.Equal(largeMessage, msgProperty.GetValue(responseValue));
    }

    [Fact]
    public void OnException_NestedExceptions_HandlesCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("内部异常");
        var outerException = new Exception("外部异常", innerException);
        var context = CreateExceptionContext(outerException);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public void OnException_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var exceptions = Enumerable.Range(0, 100)
            .Select(i => new BusinessException($"并发异常 {i}"))
            .ToList();

        var tasks = new List<Task>();

        // Act
        foreach (var exception in exceptions)
        {
            tasks.Add(Task.Run(() =>
            {
                var context = CreateExceptionContext(exception);
                _filter.OnException(context);
                Assert.True(context.ExceptionHandled);
            }));
        }

        // Assert
        Task.WaitAll(tasks.ToArray());
        Assert.True(tasks.All(t => t.IsCompletedSuccessfully));
    }

    [Fact]
    public void OnException_NullProperties_HandlesGracefully()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "null-test";
        // httpContext.Request.Method = null; // 不能设置为null
        httpContext.Request.Path = PathString.Empty;
        httpContext.Request.QueryString = QueryString.Empty;
        httpContext.Connection.RemoteIpAddress = null;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new BusinessException("空值测试")
        };

        // Act & Assert
        var ex = Record.Exception(() => _filter.OnException(context));
        Assert.Null(ex); // 不应该抛出异常
        Assert.True(context.ExceptionHandled);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void OnException_EmptyOrNullMessages_HandlesCorrectly(string message)
    {
        // Arrange
        var exception = new BusinessException(message);
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        var msgProperty = responseValue.GetType().GetProperty("msg");
        var actualMessage = msgProperty.GetValue(responseValue)?.ToString();
        
        // 空消息应该被处理为有意义的默认值
        Assert.NotNull(actualMessage);
    }

    [Fact]
    public void OnException_SpecialCharactersInMessage_HandlesCorrectly()
    {
        // Arrange
        var specialMessage = "特殊字符测试: <>&\"'`\n\r\t\0\u0001\u001F";
        var exception = new BusinessException(specialMessage);
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        var msgProperty = responseValue.GetType().GetProperty("msg");
        Assert.Equal(specialMessage, msgProperty.GetValue(responseValue));
    }

    [Fact]
    public void OnException_ResponseSerialization_ProducesValidJson()
    {
        // Arrange
        var exception = new BusinessException("JSON序列化测试");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        
        // 验证可以序列化为JSON
        var json = JsonSerializer.Serialize(responseValue);
        Assert.NotNull(json);
        // 检查Unicode编码的中文字符或原始中文字符
        Assert.True(json.Contains("JSON序列化测试") || json.Contains("JSON\\u5E8F\\u5217\\u5316\\u6D4B\\u8BD5"));
        Assert.Contains("status", json);
        Assert.Contains("msg", json);
        Assert.Contains("traceId", json);
        Assert.Contains("timestamp", json);
    }

    [Fact]
    public void OnException_LoggerFailure_DoesNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<HttpResponseExceptionFilter>>();
        mockLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new Exception("日志记录失败"));

        var filter = new HttpResponseExceptionFilter(mockLogger.Object, _mockEnvironment.Object);
        var exception = new BusinessException("日志失败测试");
        var context = CreateExceptionContext(exception);

        // Act & Assert
        var ex = Record.Exception(() => filter.OnException(context));
        Assert.Null(ex); // 即使日志记录失败，也不应该抛出异常
        Assert.True(context.ExceptionHandled);
    }
} 