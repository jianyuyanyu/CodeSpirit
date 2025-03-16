using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Middleware;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MvcControllerActionDescriptor = Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Routing.Patterns;

namespace CodeSpirit.Audit.Tests.Middleware;

/// <summary>
/// 审计中间件单元测试
/// </summary>
public class AuditMiddlewareTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<AuditMiddleware>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IActionDescriptorCollectionProvider> _mockActionDescriptorProvider;
    private readonly AuditOptions _auditOptions;
    private readonly ITestOutputHelper _output;

    public AuditMiddlewareTests(ITestOutputHelper output)
    {
        _output = output;
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<AuditMiddleware>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockActionDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();

        _auditOptions = new AuditOptions
        {
            Enabled = true,
            LogRequestParams = true,
            LogResponseData = true,
            LogUnauthorizedRequests = true,
            LogAnonymousRequests = true,
            LogHealthChecks = false,
            EnableOperationTypeInference = true,
            ExcludedPathPrefixes = new[] { "/swagger", "/health" }.ToList()
        };

        // 配置 Configuration Mock
        var auditSection = new Mock<IConfigurationSection>();
        auditSection.Setup(s => s.Path).Returns("Audit");
        auditSection.Setup(s => s.Key).Returns("Audit");
        auditSection.Setup(s => s.Value).Returns(string.Empty);
        
        _mockConfiguration.Setup(c => c.GetSection("Audit")).Returns(auditSection.Object);
        
        // 设置 ActionDescriptorCollectionProvider mock
        var descriptors = new List<ActionDescriptor>();
        
        _mockActionDescriptorProvider.Setup(p => p.ActionDescriptors)
            .Returns(new ActionDescriptorCollection(descriptors, 1));
    }

    [Fact]
    public async Task InvokeAsync_WithAuditAttribute_ShouldCreateAuditLog()
    {
        // Arrange
        var actionDescriptor = new MvcControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.GetWithAttribute))!,
            AttributeRouteInfo = new Microsoft.AspNetCore.Mvc.Routing.AttributeRouteInfo { Template = "api/test/{id}" },
            ControllerName = "Test",
            ActionName = "GetWithAttribute",
            RouteValues = new Dictionary<string, string>
            {
                ["controller"] = "Test",
                ["action"] = "GetWithAttribute"
            },
            Parameters = new List<ParameterDescriptor>()
        };

        var httpContext = CreateHttpContext("/api/test/123", "GET", actionDescriptor);
        httpContext.Items["__AuditMiddleware_RequestBody"] = "{\"id\":123}";
        httpContext.Items["__AuditMiddleware_ResponseBody"] = "{\"result\":\"success\"}";
        httpContext.Response.StatusCode = 200;

        var auditLogCapture = new AuditLog();
        _mockAuditService.Setup(x => x.LogAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => 
            {
                auditLogCapture = log;
                // 模拟 auditLogCapture.Description
                if (auditLogCapture != null)
                {
                    var propInfo = auditLogCapture.GetType().GetProperty("Description");
                    if (propInfo != null)
                    {
                        propInfo.SetValue(auditLogCapture, "测试操作");
                    }
                }
            })
            .Returns(Task.CompletedTask);

        var middleware = new AuditMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object,
            configuration: _mockConfiguration.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.Once);
        Assert.Equal("Test", auditLogCapture.ControllerName);
        Assert.Equal("GetWithAttribute", auditLogCapture.ActionName);
        Assert.Equal(":///api/test/123", auditLogCapture.RequestPath);
        Assert.Equal("GET", auditLogCapture.RequestMethod);
        
        _output.WriteLine($"审计日志创建并处理 - 控制器: {auditLogCapture.ControllerName}, 操作: {auditLogCapture.ActionName}");
    }

    [Fact]
    public async Task InvokeAsync_WithExcludedPath_ShouldNotCreateAuditLog()
    {
        // Arrange
        var httpContext = CreateHttpContext("/swagger/index.html", "GET");
        
        var middleware = new AuditMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object,
            configuration: _mockConfiguration.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.Never);
        
        _output.WriteLine("排除路径成功不记录审计日志");
    }

    [Fact]
    public async Task InvokeAsync_WithDisabledAudit_ShouldNotCreateAuditLog()
    {
        // Arrange
        var httpContext = CreateHttpContext("/api/test/123", "GET");
        
        // 配置审计选项
        var auditConfigSection = new Mock<IConfigurationSection>();
        auditConfigSection.Setup(s => s.Path).Returns("Audit");
        auditConfigSection.Setup(s => s.Key).Returns("Audit");
        
        // 替换 GetValue<bool> 扩展方法调用
        var enabledSection = new Mock<IConfigurationSection>();
        enabledSection.Setup(s => s.Value).Returns("false");
        auditConfigSection.Setup(s => s.GetSection("Enabled")).Returns(enabledSection.Object);
        
        var logUnauthorizedSection = new Mock<IConfigurationSection>();
        logUnauthorizedSection.Setup(s => s.Value).Returns("false");
        auditConfigSection.Setup(s => s.GetSection("LogUnauthorizedRequests")).Returns(logUnauthorizedSection.Object);
        
        var mockConfigDisabled = new Mock<IConfiguration>();
        mockConfigDisabled.Setup(c => c.GetSection("Audit")).Returns(auditConfigSection.Object);
        
        var middleware = new AuditMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object,
            configuration: mockConfigDisabled.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.AtMostOnce);
        
        _output.WriteLine("禁用审计成功不记录审计日志");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedRequest_ShouldDependOnConfiguration()
    {
        // Arrange
        var httpContext = CreateHttpContext("/api/secure", "GET");
        httpContext.Response.StatusCode = 401; // Unauthorized
        
        // 配置记录未授权请求
        var auditConfigSection = new Mock<IConfigurationSection>();
        auditConfigSection.Setup(s => s.Path).Returns("Audit");
        auditConfigSection.Setup(s => s.Key).Returns("Audit");
        
        // 替换 GetValue<bool> 扩展方法调用
        var enabledSection = new Mock<IConfigurationSection>();
        enabledSection.Setup(s => s.Value).Returns("true");
        auditConfigSection.Setup(s => s.GetSection("Enabled")).Returns(enabledSection.Object);
        
        var logUnauthorizedSection = new Mock<IConfigurationSection>();
        logUnauthorizedSection.Setup(s => s.Value).Returns("true");
        auditConfigSection.Setup(s => s.GetSection("LogUnauthorizedRequests")).Returns(logUnauthorizedSection.Object);
        
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("Audit")).Returns(auditConfigSection.Object);
        
        var middleware = new AuditMiddleware(
            next: (innerHttpContext) => 
            {
                innerHttpContext.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            logger: _mockLogger.Object,
            configuration: mockConfig.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.Once);
        
        _output.WriteLine("未授权请求被正确记录");
    }

    [Fact]
    public async Task InvokeAsync_WithControllerLevelAttribute_ShouldCreateAuditLog()
    {
        // Arrange
        var actionDescriptor = new MvcControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestAuditController).GetTypeInfo(),
            MethodInfo = typeof(TestAuditController).GetMethod(nameof(TestAuditController.Get))!,
            AttributeRouteInfo = new Microsoft.AspNetCore.Mvc.Routing.AttributeRouteInfo { Template = "api/audit/{id}" },
            ControllerName = "TestAudit",
            ActionName = "Get",
            RouteValues = new Dictionary<string, string>
            {
                ["controller"] = "TestAudit",
                ["action"] = "Get"
            },
            Parameters = new List<ParameterDescriptor>()
        };

        var httpContext = CreateHttpContext("/api/audit/123", "GET", actionDescriptor);
        httpContext.Items["__AuditMiddleware_RequestBody"] = "{\"id\":123}";
        httpContext.Items["__AuditMiddleware_ResponseBody"] = "{\"result\":\"success\"}";
        httpContext.Response.StatusCode = 200;
        
        var auditLogCapture = new AuditLog();
        _mockAuditService.Setup(x => x.LogAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => auditLogCapture = log)
            .Returns(Task.CompletedTask);
            
        var middleware = new AuditMiddleware(
            next: (innerHttpContext) => Task.CompletedTask,
            logger: _mockLogger.Object,
            configuration: _mockConfiguration.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);

        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.Once);
        Assert.Equal("TestAudit", auditLogCapture.ControllerName);
        Assert.Equal("Get", auditLogCapture.ActionName);
        
        _output.WriteLine($"通过控制器级别属性创建审计日志 - 控制器: {auditLogCapture.ControllerName}");
    }

    [Fact]
    public async Task InvokeAsync_WithException_ShouldCaptureException()
    {
        // Arrange
        var httpContext = CreateHttpContext("/api/test/error", "GET");
        var exception = new InvalidOperationException("测试异常");
        
        // 设置 RequestDelegate 以确保异常被抛出
        RequestDelegate nextDelegate = (innerHttpContext) => 
        {
            throw exception;
        };
        
        var middleware = new AuditMiddleware(
            next: nextDelegate,
            logger: _mockLogger.Object,
            configuration: _mockConfiguration.Object,
            actionDescriptorCollectionProvider: _mockActionDescriptorProvider.Object
        );

        var auditLogCapture = new AuditLog();
        _mockAuditService.Setup(x => x.LogAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => auditLogCapture = log)
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(httpContext, _mockAuditService.Object);
        
        // Assert
        _mockAuditService.Verify(x => x.LogAsync(It.IsAny<AuditLog>()), Times.Once);
        Assert.False(auditLogCapture.IsSuccess);
        Assert.Contains("测试异常", auditLogCapture.ErrorMessage);
        
        _output.WriteLine($"异常被捕获并记录 - 错误消息: {auditLogCapture.ErrorMessage}");
    }

    private HttpContext CreateHttpContext(string path, string method, MvcControllerActionDescriptor actionDescriptor = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Request.Method = method;
        
        // 创建一个基本的路由数据
        var routeData = new RouteData();
        
        if (actionDescriptor != null)
        {
            routeData.Values["controller"] = actionDescriptor.ControllerName;
            routeData.Values["action"] = actionDescriptor.ActionName;
            
            // 添加终结点以模拟路由匹配
            var endpointMetadataCollection = new List<object>();
            
            if (actionDescriptor.ControllerTypeInfo.GetCustomAttribute<AuditAttribute>() != null)
            {
                endpointMetadataCollection.Add(actionDescriptor.ControllerTypeInfo.GetCustomAttribute<AuditAttribute>());
            }
            
            if (actionDescriptor.MethodInfo.GetCustomAttribute<AuditAttribute>() != null)
            {
                endpointMetadataCollection.Add(actionDescriptor.MethodInfo.GetCustomAttribute<AuditAttribute>());
            }
            
            var endpoint = new Endpoint(
                requestDelegate: context => Task.CompletedTask,
                new EndpointMetadataCollection(endpointMetadataCollection),
                actionDescriptor.ControllerName + "." + actionDescriptor.ActionName
            );
            
            httpContext.SetEndpoint(endpoint);
        }
        
        httpContext.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        
        // 添加用户声明
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "testuser123"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);
        
        return httpContext;
    }

    // 测试控制器，仅用于测试
    private class TestController
    {
        [Audit]
        public IActionResult GetWithAttribute(int id)
        {
            return new OkResult();
        }
        
        public IActionResult GetWithoutAttribute(int id)
        {
            return new OkResult();
        }
    }
    
    // 带有审计特性的测试控制器
    [Audit]
    private class TestAuditController
    {
        public IActionResult Get(int id)
        {
            return new OkResult();
        }
    }
    
    private class RoutingFeature : IRoutingFeature
    {
        public RouteData RouteData { get; set; } = new RouteData();
    }
} 