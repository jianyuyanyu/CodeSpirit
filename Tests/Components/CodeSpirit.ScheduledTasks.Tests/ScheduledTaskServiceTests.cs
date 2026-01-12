using CodeSpirit.Caching.Abstractions;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using Xunit;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// 定时任务服务测试
/// </summary>
public class ScheduledTaskServiceTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ITaskExecutor> _mockTaskExecutor;
    private readonly Mock<ITaskHandlerRegistry> _mockRegistry;
    private readonly Mock<ILogger<ScheduledTaskService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ScheduledTasksOptions _options;
    private readonly ScheduledTaskService _service;

    public ScheduledTaskServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockTaskExecutor = new Mock<ITaskExecutor>();
        _mockRegistry = new Mock<ITaskHandlerRegistry>();
        _mockLogger = new Mock<ILogger<ScheduledTaskService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        
        _options = new ScheduledTasksOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromMinutes(30),
            MaxConcurrentTasks = 10,
            CacheKeyPrefix = "Test:ScheduledTasks:",
            ServiceName = "test-service"
        };

        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _mockServiceProvider.Setup(x => x.GetService(typeof(ITaskHandlerRegistry)))
            .Returns(_mockRegistry.Object);

        _service = new ScheduledTaskService(
            _mockCacheService.Object,
            _mockTaskExecutor.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object,
            optionsMock.Object);
    }

    [Fact]
    public async Task CreateTaskAsync_ValidTask_ShouldCreateSuccessfully()
    {
        // Arrange
        var task = new ScheduledTask
        {
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled
        };

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal(task.Name, result.Name);
        Assert.Equal(TaskStatus.Enabled, result.Status);
        Assert.NotNull(result.NextExecuteTime);
        
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateTaskAsync_InvalidCronExpression_ShouldThrowException()
    {
        // Arrange
        var task = new ScheduledTask
        {
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "invalid cron",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateTaskAsync(task));
    }

    [Fact]
    public async Task GetTaskAsync_ExistingTask_ShouldReturnTask()
    {
        // Arrange
        var taskId = "test-task-id";
        var expectedTask = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler"
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTask);

        // Act
        var result = await _service.GetTaskAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTask.Id, result.Id);
        Assert.Equal(expectedTask.Name, result.Name);
    }

    [Fact]
    public async Task GetTaskAsync_NonExistingTask_ShouldReturnNull()
    {
        // Arrange
        var taskId = "non-existing-task";

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        // Act
        var result = await _service.GetTaskAsync(taskId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EnableTaskAsync_ExistingTask_ShouldEnableSuccessfully()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Disabled
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.EnableTaskAsync(taskId);

        // Assert
        Assert.True(result);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.Is<ScheduledTask>(t => t.Status == TaskStatus.Enabled), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task TriggerTaskAsync_ExistingTask_ShouldTriggerSuccessfully()
    {
        // Arrange
        var taskId = "test-task-id";
        var initialExecutionCount = 5;
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            ExecutionCount = initialExecutionCount
        };

        var executionStartTime = DateTime.UtcNow;
        var execution = new TaskExecution
        {
            Id = "execution-id",
            TaskId = taskId,
            Status = TaskStatus.Running,
            StartTime = executionStartTime
        };

        // Mock GetAsync 返回任务
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Mock GetAsync 返回任务索引（用于 UpdateNextExecuteTimeAsync）
        _mockCacheService.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { taskId });

        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(false);

        _mockTaskExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        // Mock SetAsync 用于保存任务
        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TriggerTaskAsync(taskId);

        // Assert
        Assert.Equal(execution.Id, result);
        _mockTaskExecutor.Verify(x => x.ExecuteAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        
        // ✅ 验证执行次数增加
        Assert.Equal(initialExecutionCount + 1, task.ExecutionCount);
        
        // ✅ 验证最后执行时间更新
        Assert.Equal(executionStartTime, task.LastExecuteTime);
        
        // ✅ 验证任务状态被保存（UpdateTaskAsync 内部会调用 SetAsync）
        _mockCacheService.Verify(x => x.SetAsync(
            It.IsAny<string>(), 
            It.Is<ScheduledTask>(t => t.ExecutionCount == initialExecutionCount + 1 && t.LastExecuteTime == executionStartTime), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TriggerTaskAsync_TaskAlreadyRunning_ShouldThrowException()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.TriggerTaskAsync(taskId));
    }

    [Fact]
    public async Task TriggerTaskAsync_OneTimeTask_ShouldDisableAfterExecution()
    {
        // Arrange
        var taskId = "onetime-task-id";
        var executeAt = DateTime.UtcNow.AddMinutes(10);
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "一次性任务",
            Type = TaskType.OneTime,
            ExecuteAt = executeAt,
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            ExecutionCount = 0
        };

        var executionStartTime = DateTime.UtcNow;
        var execution = new TaskExecution
        {
            Id = "execution-id",
            TaskId = taskId,
            Status = TaskStatus.Completed,
            StartTime = executionStartTime
        };

        // Mock GetAsync 返回任务（会被调用多次）
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Mock GetAsync 返回任务索引
        _mockCacheService.Setup(x => x.GetAsync<List<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { taskId });

        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(false);

        _mockTaskExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        // Mock SetAsync 用于保存任务
        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TriggerTaskAsync(taskId);

        // Assert
        Assert.Equal(execution.Id, result);
        
        // ✅ 验证执行次数增加
        Assert.Equal(1, task.ExecutionCount);
        
        // ✅ 验证最后执行时间更新
        Assert.Equal(executionStartTime, task.LastExecuteTime);
        
        // ✅ 验证一次性任务执行后被禁用
        Assert.Equal(TaskStatus.Disabled, task.Status);
        
        // ✅ 验证下次执行时间被清空
        Assert.Null(task.NextExecuteTime);
        
        // ✅ 验证任务状态被保存
        _mockCacheService.Verify(x => x.SetAsync(
            It.IsAny<string>(), 
            It.Is<ScheduledTask>(t => 
                t.ExecutionCount == 1 && 
                t.Status == TaskStatus.Disabled && 
                t.NextExecuteTime == null), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAllTasksAsync_WithTasksInIndex_ShouldReturnAllTasks()
    {
        // Arrange
        var taskIds = new List<string> { "task1", "task2", "task3" };
        var tasks = taskIds.Select(id => new ScheduledTask
        {
            Id = id,
            Name = $"Test Task {id}",
            HandlerType = "TestHandler",
            CronExpression = "0 0 0 * * *", // 每天午夜执行（秒 分 时 日 月 周）
            Status = TaskStatus.Enabled,
            Type = TaskType.Cron,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        // Setup cache service to return task IDs from index
        var indexKey = $"{_options.CacheKeyPrefix}Index:All";
        _mockCacheService.Setup(x => x.GetAsync<List<string>>(indexKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskIds);

        // Setup cache service to return individual tasks
        foreach (var task in tasks)
        {
            var taskKey = $"{_options.CacheKeyPrefix}Tasks:{task.Id}";
            _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(taskKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
        }

        // Act
        var result = await _service.GetAllTasksAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Id == "task1");
        Assert.Contains(result, t => t.Id == "task2");
        Assert.Contains(result, t => t.Id == "task3");
        
        // Verify cache calls
        _mockCacheService.Verify(x => x.GetAsync<List<string>>(indexKey, It.IsAny<CancellationToken>()), Times.Once);
        foreach (var taskId in taskIds)
        {
            var taskKey = $"{_options.CacheKeyPrefix}Tasks:{taskId}";
            _mockCacheService.Verify(x => x.GetAsync<ScheduledTask>(taskKey, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task GetAllTasksAsync_WithEmptyIndex_ShouldReturnEmptyList()
    {
        // Arrange
        var indexKey = $"{_options.CacheKeyPrefix}Index:All";
        _mockCacheService.Setup(x => x.GetAsync<List<string>>(indexKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string>?)null);

        // Act
        var result = await _service.GetAllTasksAsync();

        // Assert
        Assert.Empty(result);
        _mockCacheService.Verify(x => x.GetAsync<List<string>>(indexKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldAddTaskToIndex()
    {
        // Arrange
        var task = new ScheduledTask
        {
            Name = "Test Task",
            HandlerType = "TestHandler",
            CronExpression = "0 0 0 * * *", // 每天午夜执行（秒 分 时 日 月 周）
            Status = TaskStatus.Enabled,
            Type = TaskType.Cron
        };

        var indexKey = $"{_options.CacheKeyPrefix}Index:All";
        var existingTaskIds = new List<string> { "existing-task" };
        
        _mockCacheService.Setup(x => x.GetAsync<List<string>>(indexKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTaskIds);

        // Act
        var result = await _service.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        
        // Verify task was saved
        var taskKey = $"{_options.CacheKeyPrefix}Tasks:{result.Id}";
        _mockCacheService.Verify(x => x.SetAsync(taskKey, It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify index was updated with new task ID
        _mockCacheService.Verify(x => x.SetAsync(indexKey, 
            It.Is<List<string>>(list => list.Contains(result.Id) && list.Contains("existing-task")), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #region CreateTaskAsync - 注册服务映射测试

    [Fact]
    public async Task CreateTaskAsync_WithTargetService_ShouldRegisterTaskWithTargetService()
    {
        // Arrange
        var targetService = "target-service";
        var task = new ScheduledTask
        {
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = targetService // 指定目标服务
        };

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), targetService, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        
        // 验证注册表被调用，使用任务的 TargetService
        _mockRegistry.Verify(x => x.RegisterTaskServiceAsync(
            result.Id, 
            targetService, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WithoutTargetService_ShouldRegisterTaskWithCurrentServiceName()
    {
        // Arrange
        var task = new ScheduledTask
        {
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = null // 未指定目标服务
        };

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), _options.ServiceName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        
        // 验证注册表被调用，使用当前服务的 ServiceName
        _mockRegistry.Verify(x => x.RegisterTaskServiceAsync(
            result.Id, 
            _options.ServiceName, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_RegistryNotAvailable_ShouldNotThrowException()
    {
        // Arrange
        // 创建一个没有注册表的服务实例
        var mockServiceProviderWithoutRegistry = new Mock<IServiceProvider>();
        mockServiceProviderWithoutRegistry.Setup(x => x.GetService(typeof(ITaskHandlerRegistry)))
            .Returns((ITaskHandlerRegistry?)null);

        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        var serviceWithoutRegistry = new ScheduledTaskService(
            _mockCacheService.Object,
            _mockTaskExecutor.Object,
            mockServiceProviderWithoutRegistry.Object,
            _mockLogger.Object,
            optionsMock.Object);

        var task = new ScheduledTask
        {
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled
        };

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act - 不应该抛出异常
        var result = await serviceWithoutRegistry.CreateTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
    }

    #endregion

    #region UpdateTaskAsync - 更新服务映射测试

    [Fact]
    public async Task UpdateTaskAsync_TargetServiceChanged_ShouldUpdateServiceMapping()
    {
        // Arrange
        var taskId = "test-task-id";
        var oldTargetService = "old-service";
        var newTargetService = "new-service";
        
        var existingTask = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = oldTargetService
        };

        var updatedTask = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = newTargetService // TargetService 发生变化
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(taskId, newTargetService, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateTaskAsync(updatedTask);

        // Assert
        Assert.NotNull(result);
        
        // 验证注册表被调用，更新服务映射
        _mockRegistry.Verify(x => x.RegisterTaskServiceAsync(
            taskId, 
            newTargetService, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_TargetServiceNotChanged_ShouldNotUpdateServiceMapping()
    {
        // Arrange
        var taskId = "test-task-id";
        var targetService = "same-service";
        
        var existingTask = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = targetService
        };

        var updatedTask = new ScheduledTask
        {
            Id = taskId,
            Name = "测试任务更新",  // 只修改名称
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            TargetService = targetService // TargetService 未变化
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateTaskAsync(updatedTask);

        // Assert
        Assert.NotNull(result);
        
        // 验证注册表未被调用（因为 TargetService 未变化）
        _mockRegistry.Verify(x => x.RegisterTaskServiceAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region LoadTasksFromConfigurationAsync - 配置文件任务覆盖测试

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_NewTask_ShouldCreateWithConfigValues()
    {
        // Arrange
        var taskDefinition = new TaskDefinition
        {
            Id = "config-task-1",
            Name = "配置任务1",
            Description = "测试配置任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Enabled = true,
            Group = "test-group",
            Priority = 8
        };

        _options.Tasks.Add(taskDefinition);

        // 模拟任务不存在（新任务）
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var loadedCount = await _service.LoadTasksFromConfigurationAsync();

        // Assert
        Assert.Equal(1, loadedCount);

        // 验证保存的任务包含配置文件中的值
        _mockCacheService.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:config-task-1")),
            It.Is<ScheduledTask>(t => 
                t.Id == "config-task-1" &&
                t.Name == "配置任务1" &&
                t.Description == "测试配置任务" &&
                t.HandlerType == "TestHandler" &&
                t.Group == "test-group" &&
                t.Priority == 8 &&
                t.IsFromConfiguration == true &&
                t.TargetService == _options.ServiceName), // 应使用当前服务名称
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_ExistingTask_ShouldOverrideConfigButPreserveRuntimeState()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var originalCreatedBy = "original-creator";
        
        var taskDefinition = new TaskDefinition
        {
            Id = "config-task-existing",
            Name = "更新后的任务名称",           // 配置文件中更新了名称
            Description = "更新后的描述",         // 配置文件中更新了描述
            Type = TaskType.Cron,
            CronExpression = "0 */10 * * * *",   // 配置文件中更新了 Cron 表达式
            HandlerType = "NewTestHandler",       // 配置文件中更新了处理器
            Enabled = true,
            Group = "new-group",                  // 配置文件中更新了分组
            Priority = 9                          // 配置文件中更新了优先级
        };

        _options.Tasks.Add(taskDefinition);

        // 模拟已存在的任务（包含运行时状态）
        var existingTask = new ScheduledTask
        {
            Id = "config-task-existing",
            Name = "原始任务名称",
            Description = "原始描述",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "OldTestHandler",
            Status = TaskStatus.Disabled,           // 运行时状态：用户手动禁用
            ExecutionCount = 42,                    // 运行时状态：已执行42次
            LastExecuteTime = DateTime.UtcNow.AddHours(-1), // 运行时状态：上次执行时间
            CreatedAt = originalCreatedAt,          // 运行时状态：原始创建时间
            CreatedBy = originalCreatedBy,          // 运行时状态：原始创建者
            IsFromConfiguration = true,
            Group = "old-group",
            Priority = 5
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(
            It.Is<string>(key => key.Contains("Tasks:config-task-existing")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        ScheduledTask? savedTask = null;
        _mockCacheService.Setup(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:config-task-existing")),
            It.IsAny<ScheduledTask>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Callback<string, ScheduledTask, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>((key, task, options, ct) => savedTask = task)
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var loadedCount = await _service.LoadTasksFromConfigurationAsync();

        // Assert
        Assert.Equal(1, loadedCount);
        Assert.NotNull(savedTask);

        // ✅ 验证配置项被覆盖为最新值
        Assert.Equal("更新后的任务名称", savedTask.Name);
        Assert.Equal("更新后的描述", savedTask.Description);
        Assert.Equal("0 */10 * * * *", savedTask.CronExpression);
        Assert.Equal("NewTestHandler", savedTask.HandlerType);
        Assert.Equal("new-group", savedTask.Group);
        Assert.Equal(9, savedTask.Priority);
        Assert.True(savedTask.IsFromConfiguration);

        // ✅ 验证运行时状态被保留
        Assert.Equal(TaskStatus.Disabled, savedTask.Status);       // 保留用户手动禁用的状态
        Assert.Equal(42, savedTask.ExecutionCount);                 // 保留执行次数
        Assert.NotNull(savedTask.LastExecuteTime);                  // 保留上次执行时间
        Assert.Equal(originalCreatedAt, savedTask.CreatedAt);       // 保留原始创建时间
        Assert.Equal(originalCreatedBy, savedTask.CreatedBy);       // 保留原始创建者
    }

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_ExistingTask_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        var originalUpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var taskDefinition = new TaskDefinition
        {
            Id = "config-task-timestamp",
            Name = "时间戳测试任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Enabled = true
        };

        _options.Tasks.Add(taskDefinition);

        var existingTask = new ScheduledTask
        {
            Id = "config-task-timestamp",
            Name = "原始任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Status = TaskStatus.Enabled,
            CreatedAt = originalUpdatedAt,
            UpdatedAt = originalUpdatedAt,
            IsFromConfiguration = true
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(
            It.Is<string>(key => key.Contains("Tasks:config-task-timestamp")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        ScheduledTask? savedTask = null;
        _mockCacheService.Setup(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:config-task-timestamp")),
            It.IsAny<ScheduledTask>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Callback<string, ScheduledTask, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>((key, task, options, ct) => savedTask = task)
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var beforeLoad = DateTime.UtcNow;

        // Act
        await _service.LoadTasksFromConfigurationAsync();

        var afterLoad = DateTime.UtcNow;

        // Assert
        Assert.NotNull(savedTask);
        Assert.Equal(originalUpdatedAt, savedTask.CreatedAt);  // CreatedAt 保持不变
        Assert.True(savedTask.UpdatedAt >= beforeLoad);         // UpdatedAt 应该被更新
        Assert.True(savedTask.UpdatedAt <= afterLoad);
    }

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_WithTargetServiceInConfig_ShouldUseConfigTargetService()
    {
        // Arrange
        var taskDefinition = new TaskDefinition
        {
            Id = "config-task-with-target",
            Name = "指定目标服务的任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Enabled = true,
            TargetService = "specific-target-service"  // 配置文件中指定了目标服务
        };

        _options.Tasks.Add(taskDefinition);

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        ScheduledTask? savedTask = null;
        _mockCacheService.Setup(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:config-task-with-target")),
            It.IsAny<ScheduledTask>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Callback<string, ScheduledTask, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>((key, task, options, ct) => savedTask = task)
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LoadTasksFromConfigurationAsync();

        // Assert
        Assert.NotNull(savedTask);
        Assert.Equal("specific-target-service", savedTask.TargetService);  // 应使用配置中指定的目标服务
    }

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_WithoutTargetServiceInConfig_ShouldUseCurrentServiceName()
    {
        // Arrange
        var taskDefinition = new TaskDefinition
        {
            Id = "config-task-no-target",
            Name = "未指定目标服务的任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "TestHandler",
            Enabled = true,
            TargetService = null  // 配置文件中未指定目标服务
        };

        _options.Tasks.Add(taskDefinition);

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        ScheduledTask? savedTask = null;
        _mockCacheService.Setup(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:config-task-no-target")),
            It.IsAny<ScheduledTask>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Callback<string, ScheduledTask, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>((key, task, options, ct) => savedTask = task)
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LoadTasksFromConfigurationAsync();

        // Assert
        Assert.NotNull(savedTask);
        Assert.Equal(_options.ServiceName, savedTask.TargetService);  // 应使用当前服务名称
    }

    [Fact]
    public async Task LoadTasksFromConfigurationAsync_MultipleTasksWithMixedExistence_ShouldHandleCorrectly()
    {
        // Arrange
        var newTaskDef = new TaskDefinition
        {
            Id = "new-config-task",
            Name = "新配置任务",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "NewHandler",
            Enabled = true
        };

        var existingTaskDef = new TaskDefinition
        {
            Id = "existing-config-task",
            Name = "更新后的名称",
            Type = TaskType.Cron,
            CronExpression = "0 */10 * * * *",
            HandlerType = "UpdatedHandler",
            Enabled = true
        };

        _options.Tasks.Add(newTaskDef);
        _options.Tasks.Add(existingTaskDef);

        var existingTask = new ScheduledTask
        {
            Id = "existing-config-task",
            Name = "原始名称",
            Type = TaskType.Cron,
            CronExpression = "0 */5 * * * *",
            HandlerType = "OldHandler",
            Status = TaskStatus.Disabled,
            ExecutionCount = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsFromConfiguration = true
        };

        // 新任务返回 null
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(
            It.Is<string>(key => key.Contains("Tasks:new-config-task")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        // 已存在的任务返回现有值
        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(
            It.Is<string>(key => key.Contains("Tasks:existing-config-task")), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTask);

        var savedTasks = new Dictionary<string, ScheduledTask>();
        _mockCacheService.Setup(x => x.SetAsync(
            It.Is<string>(key => key.Contains("Tasks:")),
            It.IsAny<ScheduledTask>(), 
            It.IsAny<CodeSpirit.Caching.Models.CacheOptions>(), 
            It.IsAny<CancellationToken>()))
            .Callback<string, ScheduledTask, CodeSpirit.Caching.Models.CacheOptions, CancellationToken>((key, task, options, ct) => savedTasks[task.Id] = task)
            .Returns(Task.CompletedTask);

        _mockRegistry.Setup(x => x.RegisterTaskServiceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var loadedCount = await _service.LoadTasksFromConfigurationAsync();

        // Assert
        Assert.Equal(2, loadedCount);
        Assert.Equal(2, savedTasks.Count);

        // 验证新任务
        Assert.True(savedTasks.ContainsKey("new-config-task"));
        var newTask = savedTasks["new-config-task"];
        Assert.Equal("新配置任务", newTask.Name);
        Assert.Equal(0, newTask.ExecutionCount);  // 新任务执行次数为 0

        // 验证已存在任务
        Assert.True(savedTasks.ContainsKey("existing-config-task"));
        var updatedTask = savedTasks["existing-config-task"];
        Assert.Equal("更新后的名称", updatedTask.Name);              // 名称被覆盖
        Assert.Equal("0 */10 * * * *", updatedTask.CronExpression);  // Cron 被覆盖
        Assert.Equal("UpdatedHandler", updatedTask.HandlerType);     // 处理器被覆盖
        Assert.Equal(TaskStatus.Disabled, updatedTask.Status);       // 状态保留
        Assert.Equal(100, updatedTask.ExecutionCount);               // 执行次数保留
    }

    #endregion
}
