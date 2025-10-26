using CodeSpirit.Aggregator.Middlewares;
using CodeSpirit.Aggregator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using CodeSpirit.Core.Attributes;
using Xunit;

namespace CodeSpirit.Aggregator.Tests.Middlewares
{
    /// <summary>
    /// AggregationHeaderMiddleware 中间件的单元测试类
    /// 主要测试中间件在不同场景下处理聚合头部信息的行为
    /// </summary>
    public class AggregationHeaderMiddlewareTests
    {
        private readonly Mock<IAggregationHeaderService> _headerServiceMock;
        private readonly Mock<ILogger<AggregationHeaderMiddleware>> _loggerMock;
        private readonly DefaultHttpContext _httpContext;

        /// <summary>
        /// 测试类构造函数，初始化测试所需的模拟对象和HTTP上下文
        /// </summary>
        public AggregationHeaderMiddlewareTests()
        {
            _headerServiceMock = new Mock<IAggregationHeaderService>();
            _loggerMock = new Mock<ILogger<AggregationHeaderMiddleware>>();
            _httpContext = new DefaultHttpContext();
        }

        /// <summary>
        /// 测试场景：当响应类型存在时，中间件应该正确添加聚合头部信息
        /// 验证点：
        /// 1. 中间件能够正确识别响应类型
        /// 2. 调用 HeaderService 生成聚合头部
        /// 3. 在响应头中添加正确的 X-Aggregate-Keys
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WhenResponseTypeExists_ShouldAddAggregationHeader()
        {
            // Arrange
            var responseBody = new MemoryStream();
            _httpContext.Response.Body = responseBody;

            var endpoint = CreateEndpoint(typeof(TestResponse));
            _httpContext.SetEndpoint(endpoint);

            _headerServiceMock
                .Setup(x => x.GenerateAggregationHeader(It.IsAny<Type>()))
                .Returns("test-header");

            var middleware = new AggregationHeaderMiddleware(
                async (context) =>
                {
                    var bytes = Encoding.UTF8.GetBytes("test response");
                    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                },
                _headerServiceMock.Object,
                _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal("test-header", _httpContext.Response.Headers["X-Aggregate-Keys"]);
        }

        /// <summary>
        /// 测试场景：当没有响应类型时，中间件不应添加头部信息
        /// 验证点：
        /// 1. 确保在没有响应类型的情况下不会添加聚合头部
        /// 2. 中间件正确处理无类型场景
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WhenNoResponseType_ShouldNotAddHeader()
        {
            // Arrange
            var responseBody = new MemoryStream();
            _httpContext.Response.Body = responseBody;

            var middleware = new AggregationHeaderMiddleware(
                async (context) =>
                {
                    var bytes = Encoding.UTF8.GetBytes("test response");
                    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                },
                _headerServiceMock.Object,
                _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.False(_httpContext.Response.Headers.ContainsKey("X-Aggregate-Keys"));
        }

        /// <summary>
        /// 测试场景：处理 ActionResult<T> 类型的响应
        /// 验证点：
        /// 1. 中间件能够正确解析 ActionResult<T> 中的实际类型
        /// 2. 确保传递给 HeaderService 的是正确的内部类型
        /// 3. 验证类型提取的准确性
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithActionResultType_ShouldExtractCorrectType()
        {
            // Arrange
            var responseBody = new MemoryStream();
            _httpContext.Response.Body = responseBody;

            var endpoint = CreateEndpoint(typeof(ActionResult<TestResponse>));
            _httpContext.SetEndpoint(endpoint);

            Type capturedType = null;
            _headerServiceMock
                .Setup(x => x.GenerateAggregationHeader(It.IsAny<Type>()))
                .Callback<Type>(t => capturedType = t)
                .Returns("test-header");

            var middleware = new AggregationHeaderMiddleware(
                async (context) =>
                {
                    var bytes = Encoding.UTF8.GetBytes("test response");
                    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                },
                _headerServiceMock.Object,
                _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.Equal(typeof(TestResponse), capturedType);
        }

        /// <summary>
        /// 测试场景：处理包含聚合字段的DTO响应
        /// 验证点：
        /// 1. 中间件能够正确处理包含聚合字段的DTO
        /// 2. 确保生成正确的聚合头部信息
        /// 3. 验证Base64编码的处理
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithDtoResponse_ShouldAddEncodedHeader()
        {
            // Arrange
            var responseBody = new MemoryStream();
            _httpContext.Response.Body = responseBody;

            // 创建一个特定的控制器方法用于DTO测试
            var methodInfo = typeof(TestController)
                .GetMethod(nameof(TestController.TestDtoAction))!;

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo,
                RouteValues = new Dictionary<string, string>
                {
                    { "controller", "Test" },
                    { "action", "TestDtoAction" }
                }
            };

            _httpContext.SetEndpoint(new Endpoint(
                context => Task.CompletedTask,
                new EndpointMetadataCollection(controllerActionDescriptor),
                "Test DTO endpoint"
            ));

            // 模拟生成Base64编码的聚合头部
            var headerContent = "createdBy=/identity/api/identity/users/{value}.data.name#用户: {field}";
            var expectedHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerContent));
            
            _headerServiceMock
                .Setup(x => x.GenerateAggregationHeader(It.IsAny<Type>()))
                .Returns(expectedHeader)
                .Verifiable();

            var middleware = new AggregationHeaderMiddleware(
                async (context) =>
                {
                    var response = new TestPublishHistoryDto { Id = 1, CreatedBy = "admin" };
                    var json = System.Text.Json.JsonSerializer.Serialize(response);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                },
                _headerServiceMock.Object,
                _loggerMock.Object
            );

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            _headerServiceMock.Verify(x => x.GenerateAggregationHeader(It.Is<Type>(t => 
                t == typeof(ActionResult<TestPublishHistoryDto>) || 
                t == typeof(TestPublishHistoryDto))), Times.Once);
            Assert.Equal(expectedHeader, _httpContext.Response.Headers["X-Aggregate-Keys"]);
            
            // 验证响应内容
            responseBody.Position = 0;
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            Assert.Contains("admin", responseContent);
            
            // 记录调试信息
            _loggerMock.Object.LogInformation($"Expected header: {expectedHeader}");
            _loggerMock.Object.LogInformation($"Actual header: {_httpContext.Response.Headers["X-Aggregate-Keys"]}");
            _loggerMock.Object.LogInformation($"Response content: {responseContent}");
        }

        /// <summary>
        /// 辅助方法：创建用于测试的终结点
        /// 用于模拟包含特定返回类型的控制器操作终结点
        /// </summary>
        /// <param name="returnType">期望的返回类型</param>
        /// <returns>配置好的测试终结点</returns>
        private static Endpoint CreateEndpoint(Type returnType)
        {
            var methodInfo = typeof(TestController)
                .GetMethod(nameof(TestController.TestAction))!;

            var controllerActionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = methodInfo
            };

            return new Endpoint(
                context => Task.CompletedTask,
                new EndpointMetadataCollection(controllerActionDescriptor),
                "Test endpoint"
            );
        }

        /// <summary>
        /// 测试用的响应类型
        /// </summary>
        private class TestResponse { }

        /// <summary>
        /// 测试用的控制器类
        /// </summary>
        private class TestController
        {
            public TestResponse TestAction() => new TestResponse();
            
            public ActionResult<TestPublishHistoryDto> TestDtoAction() => 
                new TestPublishHistoryDto { Id = 1, CreatedBy = "admin" };
        }

        /// <summary>
        /// 测试用的DTO类型
        /// </summary>
        private class TestPublishHistoryDto
        {
            [DisplayName("ID")]
            public int Id { get; set; }

            [DisplayName("发布人")]
            [AggregateField(dataSource: "/identity/api/identity/users/{value}.data.name", template: "用户: {field}")]
            public string CreatedBy { get; set; }
        }
    }
} 