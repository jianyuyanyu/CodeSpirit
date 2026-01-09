using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// 任务处理器注册表测试
/// </summary>
public class TaskHandlerRegistryTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<TaskHandlerRegistry>> _mockLogger;
    private readonly ScheduledTasksOptions _options;
    private readonly TaskHandlerRegistry _registry;

    public TaskHandlerRegistryTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<TaskHandlerRegistry>>();
        
        _options = new ScheduledTasksOptions
        {
            CacheKeyPrefix = "Test:ScheduledTasks:"
        };

        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        // 设置服务作用域工厂
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        
        // Moq 不支持扩展方法，需要设置 GetService 方法
        _mockServiceProvider.Setup(x => x.GetService(typeof(ICacheService)))
            .Returns(_mockCacheService.Object);

        _registry = new TaskHandlerRegistry(
            _mockServiceScopeFactory.Object,
            optionsMock.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterHandlersAsync_ValidHandlers_ShouldRegisterSuccessfully()
    {
        // Arrange
        var serviceName = "test-service";
        var handlerTypes = new List<string> 
        { 
            "TestHandler1", 
            "TestHandler2" 
        };

        _mockCacheService.Setup(x => x.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _registry.RegisterHandlersAsync(serviceName, handlerTypes);

        // Assert
        var expectedKey = $"{_options.CacheKeyPrefix}ScheduledTasks:Registry:{serviceName}";
        _mockCacheService.Verify(x => x.SetAsync(
            expectedKey, 
            It.IsAny<string>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterHandlersAsync_EmptyHandlers_ShouldLogWarning()
    {
        // Arrange
        var serviceName = "test-service";
        var handlerTypes = new List<string>();

        // Act
        await _registry.RegisterHandlersAsync(serviceName, handlerTypes);

        // Assert
        _mockCacheService.Verify(x => x.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterTaskServiceAsync_ValidTask_ShouldRegisterSuccessfully()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "test-service";

        _mockCacheService.Setup(x => x.SetAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _registry.RegisterTaskServiceAsync(taskId, serviceName);

        // Assert
        var expectedKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Verify(x => x.SetAsync(
            expectedKey, 
            serviceName, 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTaskServiceNameAsync_ExistingTask_ShouldReturnServiceName()
    {
        // Arrange
        var taskId = "test-task-id";
        var expectedServiceName = "test-service";

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedServiceName);

        // Act
        var result = await _registry.GetTaskServiceNameAsync(taskId);

        // Assert
        Assert.Equal(expectedServiceName, result);
        
        var expectedKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Verify(x => x.GetAsync<string>(
            expectedKey, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTaskServiceNameAsync_NonExistingTask_ShouldReturnNull()
    {
        // Arrange
        var taskId = "non-existing-task";

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _registry.GetTaskServiceNameAsync(taskId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_TaskOwnedByService_ShouldReturnTrue()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "test-service";

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceName);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_TaskNotOwnedByService_ShouldReturnFalse()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "test-service";
        var otherServiceName = "other-service";

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherServiceName);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetServiceHandlersAsync_ExistingService_ShouldReturnHandlers()
    {
        // Arrange
        var serviceName = "test-service";
        var expectedHandlers = new List<string> { "Handler1", "Handler2" };
        var handlersJson = System.Text.Json.JsonSerializer.Serialize(expectedHandlers);

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlersJson);

        // Act
        var result = await _registry.GetServiceHandlersAsync(serviceName);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Handler1", result);
        Assert.Contains("Handler2", result);
    }

    [Fact]
    public async Task GetServiceHandlersAsync_NonExistingService_ShouldReturnEmptyList()
    {
        // Arrange
        var serviceName = "non-existing-service";

        _mockCacheService.Setup(x => x.GetAsync<string>(
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _registry.GetServiceHandlersAsync(serviceName);

        // Assert
        Assert.Empty(result);
    }

    #region IsTaskOwnedByServiceAsync - TargetService 字段支持测试

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_NoRegistration_TaskHasMatchingTargetService_ShouldReturnTrue()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "target-service";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            HandlerType = "TestHandler",
            TargetService = serviceName // 任务指定了目标服务
        };

        // Redis 中没有注册信息
        var taskServiceKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<string>(taskServiceKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // 从任务缓存中获取任务
        var taskCacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(taskCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_NoRegistration_TaskHasDifferentTargetService_ShouldReturnFalse()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "current-service";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            HandlerType = "TestHandler",
            TargetService = "other-service" // 任务指定了其他目标服务
        };

        // Redis 中没有注册信息
        var taskServiceKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<string>(taskServiceKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // 从任务缓存中获取任务
        var taskCacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(taskCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_NoRegistration_TaskHasNoTargetService_ShouldAutoRegisterAndReturnTrue()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "current-service";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            HandlerType = "TestHandler",
            TargetService = null // 任务没有指定目标服务
        };

        // Redis 中没有注册信息
        var taskServiceKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<string>(taskServiceKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // 从任务缓存中获取任务
        var taskCacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(taskCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // 设置自动注册的 mock
        _mockCacheService.Setup(x => x.SetAsync(
            taskServiceKey,
            serviceName,
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.True(result);
        
        // 验证自动注册被调用
        _mockCacheService.Verify(x => x.SetAsync(
            taskServiceKey,
            serviceName,
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_NoRegistration_TaskNotInCache_ShouldReturnFalse()
    {
        // Arrange
        var taskId = "non-existing-task";
        var serviceName = "current-service";

        // Redis 中没有注册信息
        var taskServiceKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<string>(taskServiceKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // 任务不在缓存中
        var taskCacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(taskCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTaskOwnedByServiceAsync_HasRegistration_ShouldUseRegistrationNotTargetService()
    {
        // Arrange
        var taskId = "test-task-id";
        var serviceName = "registered-service";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            HandlerType = "TestHandler",
            TargetService = "different-service" // 任务的 TargetService 与注册的不同
        };

        // Redis 中有注册信息，应该使用注册信息而不是 TargetService
        var taskServiceKey = $"{_options.CacheKeyPrefix}ScheduledTasks:TaskService:{taskId}";
        _mockCacheService.Setup(x => x.GetAsync<string>(taskServiceKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceName);

        // Act
        var result = await _registry.IsTaskOwnedByServiceAsync(taskId, serviceName);

        // Assert
        Assert.True(result);
        
        // 验证没有访问任务缓存（因为已经从注册信息中获取到了）
        var taskCacheKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
        _mockCacheService.Verify(x => x.GetAsync<ScheduledTask>(taskCacheKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}

