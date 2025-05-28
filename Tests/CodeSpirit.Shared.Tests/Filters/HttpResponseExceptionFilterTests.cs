using CodeSpirit.Core;
using CodeSpirit.Shared.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using System.Text.Json;

namespace CodeSpirit.Shared.Tests.Filters;

/// <summary>
/// HttpResponseExceptionFilter 单元测试
/// </summary>
public class HttpResponseExceptionFilterTests
{
    private readonly Mock<ILogger<HttpResponseExceptionFilter>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly HttpResponseExceptionFilter _filter;

    public HttpResponseExceptionFilterTests()
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
        httpContext.TraceIdentifier = "test-trace-id";
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";
        httpContext.Request.QueryString = new QueryString("?param=value");
        httpContext.Request.Headers.UserAgent = "Test-Agent";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    /// <summary>
    /// 验证响应结果
    /// </summary>
    /// <param name="result">响应结果</param>
    /// <param name="expectedStatusCode">期望的状态码</param>
    /// <param name="expectedMessage">期望的消息</param>
    private void AssertAmisResponse(IActionResult result, int expectedStatusCode, string expectedMessage)
    {
        Assert.NotNull(result);
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);

        var responseValue = objectResult.Value;
        Assert.NotNull(responseValue);

        // 使用反射获取属性值
        var statusProperty = responseValue.GetType().GetProperty("status");
        var msgProperty = responseValue.GetType().GetProperty("msg");
        var traceIdProperty = responseValue.GetType().GetProperty("traceId");
        var timestampProperty = responseValue.GetType().GetProperty("timestamp");

        Assert.NotNull(statusProperty);
        Assert.NotNull(msgProperty);
        Assert.NotNull(traceIdProperty);
        Assert.NotNull(timestampProperty);

        Assert.Equal(expectedStatusCode, statusProperty.GetValue(responseValue));
        Assert.Equal(expectedMessage, msgProperty.GetValue(responseValue));
        Assert.Equal("test-trace-id", traceIdProperty.GetValue(responseValue));
        Assert.NotNull(timestampProperty.GetValue(responseValue));
    }

    [Fact]
    public void OnException_BusinessException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new BusinessException("业务逻辑错误");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 400, "业务逻辑错误");

        // 验证日志记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("BusinessException")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_ValidationException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new ValidationException("数据验证失败");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 422, "数据验证失败");
    }

    [Fact]
    public void OnException_AppServiceException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new AppServiceException(404, "资源未找到");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 404, "资源未找到");
    }

    [Fact]
    public void OnException_AppServiceExceptionWithHighCode_ReturnsInternalServerError()
    {
        // Arrange
        var exception = new AppServiceException(1001, "内部服务错误");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 500, "内部服务错误");
    }

    [Fact]
    public void OnException_ArgumentNullException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new ArgumentNullException("param", "参数不能为空");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 400, "请求参数不能为空");

        // 验证日志级别
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_ArgumentException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new ArgumentException("参数格式错误");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 400, "请求参数无效");
    }

    [Fact]
    public void OnException_UnauthorizedAccessException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("访问被拒绝");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 403, "访问被拒绝，权限不足");

        // 验证日志级别
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_FileNotFoundException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new FileNotFoundException("文件未找到");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 404, "请求的资源未找到");
    }

    [Fact]
    public void OnException_KeyNotFoundException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new KeyNotFoundException("键未找到");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 404, "请求的数据未找到");
    }

    [Fact]
    public void OnException_NotImplementedException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new NotImplementedException("功能未实现");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 501, "功能尚未实现");
    }

    [Fact]
    public void OnException_TimeoutException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new TimeoutException("操作超时");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 504, "请求超时，请稍后重试");
    }

    [Fact]
    public void OnException_OperationCanceledException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new OperationCanceledException("操作被取消");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 499, "请求已取消");

        // 验证日志级别
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_DBConcurrencyException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new DBConcurrencyException("并发冲突");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 409, "数据并发冲突，请刷新后重试");
    }

    [Fact]
    public void OnException_DbUpdateException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new DbUpdateException("数据库更新失败");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 409, "数据操作失败");
    }

    [Fact]
    public void OnException_DbUpdateExceptionWithUniqueConstraint_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var innerException = new Exception("UNIQUE constraint failed");
        var exception = new DbUpdateException("数据库更新失败", innerException);
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 409, "数据已存在，不能重复添加");
    }

    [Fact]
    public void OnException_DbUpdateExceptionWithForeignKeyConstraint_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var innerException = new Exception("FOREIGN KEY constraint failed");
        var exception = new DbUpdateException("数据库更新失败", innerException);
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 409, "数据关联约束冲突");
    }

    [Fact]
    public void OnException_InvalidOperationException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new InvalidOperationException("无效操作");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 409, "当前操作无效");
    }

    [Fact]
    public void OnException_FormatException_ReturnsCorrectAmisResponse()
    {
        // Arrange
        var exception = new FormatException("格式错误");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 400, "数据格式错误");
    }

    [Fact]
    public void OnException_GenericException_InDevelopment_ReturnsDetailedError()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
        var exception = new Exception("通用异常");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 500, "通用异常");

        // 验证日志级别
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_GenericException_InProduction_ReturnsGenericError()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        var exception = new Exception("通用异常");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        AssertAmisResponse(context.Result, 500, "服务器内部错误");
    }

    [Fact]
    public void OnException_LogsRequestInformation()
    {
        // Arrange
        var exception = new BusinessException("测试异常");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GET") && 
                                             v.ToString().Contains("/api/test") &&
                                             v.ToString().Contains("test-trace-id")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_ResponseContainsCorrectAmisFormat()
    {
        // Arrange
        var exception = new BusinessException("测试消息");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        
        // 验证响应包含所有必需的 Amis 字段
        var responseType = responseValue.GetType();
        Assert.NotNull(responseType.GetProperty("status"));
        Assert.NotNull(responseType.GetProperty("msg"));
        Assert.NotNull(responseType.GetProperty("data"));
        Assert.NotNull(responseType.GetProperty("traceId"));
        Assert.NotNull(responseType.GetProperty("timestamp"));

        // 验证 data 字段为 null（错误响应）
        var dataProperty = responseType.GetProperty("data");
        Assert.Null(dataProperty.GetValue(responseValue));
    }

    [Theory]
    [InlineData(typeof(ArgumentNullException), LogLevel.Warning)]
    [InlineData(typeof(ArgumentException), LogLevel.Warning)]
    [InlineData(typeof(UnauthorizedAccessException), LogLevel.Warning)]
    [InlineData(typeof(FileNotFoundException), LogLevel.Warning)]
    [InlineData(typeof(BusinessException), LogLevel.Information)]
    [InlineData(typeof(ValidationException), LogLevel.Information)]
    [InlineData(typeof(OperationCanceledException), LogLevel.Information)]
    [InlineData(typeof(Exception), LogLevel.Error)]
    public void OnException_LogsWithCorrectLevel(Type exceptionType, LogLevel expectedLogLevel)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "测试消息");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void OnException_ValidationException_UsesAmisCompatibleFormat()
    {
        // Arrange
        var exception = new ValidationException("验证失败");
        var context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.True(context.ExceptionHandled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(422, objectResult.StatusCode);

        // 验证使用了 Amis 兼容的响应格式
        var responseValue = objectResult.Value;
        var statusProperty = responseValue.GetType().GetProperty("status");
        Assert.Equal(422, statusProperty.GetValue(responseValue));
    }

    [Fact]
    public void OnException_PreservesTraceId()
    {
        // Arrange
        var customTraceId = "custom-trace-12345";
        var exception = new BusinessException("测试异常");
        var context = CreateExceptionContext(exception);
        context.HttpContext.TraceIdentifier = customTraceId;

        // Act
        _filter.OnException(context);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        var traceIdProperty = responseValue.GetType().GetProperty("traceId");
        Assert.Equal(customTraceId, traceIdProperty.GetValue(responseValue));
    }

    [Fact]
    public void OnException_TimestampIsRecent()
    {
        // Arrange
        var exception = new BusinessException("测试异常");
        var context = CreateExceptionContext(exception);
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        _filter.OnException(context);

        // Assert
        var afterTime = DateTimeOffset.UtcNow;
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        var responseValue = objectResult.Value;
        var timestampProperty = responseValue.GetType().GetProperty("timestamp");
        var timestampValue = timestampProperty.GetValue(responseValue).ToString();
        
        Assert.NotNull(timestampValue);
        // Amis格式使用 "yyyy-MM-dd HH:mm:ss" 格式
        Assert.True(DateTime.TryParseExact(timestampValue, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var timestamp));
        var timestampOffset = new DateTimeOffset(timestamp, TimeSpan.Zero);
        Assert.True(timestampOffset >= beforeTime.AddSeconds(-1) && timestampOffset <= afterTime.AddSeconds(1));
    }
} 