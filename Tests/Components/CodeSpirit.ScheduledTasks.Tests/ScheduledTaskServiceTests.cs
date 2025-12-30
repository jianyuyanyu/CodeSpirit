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
    private readonly Mock<ILogger<ScheduledTaskService>> _mockLogger;
    private readonly ScheduledTasksOptions _options;
    private readonly ScheduledTaskService _service;

    public ScheduledTaskServiceTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockTaskExecutor = new Mock<ITaskExecutor>();
        _mockLogger = new Mock<ILogger<ScheduledTaskService>>();
        
        _options = new ScheduledTasksOptions
        {
            Enabled = true,
            DefaultTimeout = TimeSpan.FromMinutes(30),
            MaxConcurrentTasks = 10,
            CacheKeyPrefix = "Test:ScheduledTasks:"
        };

        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(ITaskHandlerRegistry)))
            .Returns((ITaskHandlerRegistry?)null);

        _service = new ScheduledTaskService(
            _mockCacheService.Object,
            _mockTaskExecutor.Object,
            mockServiceProvider.Object,
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
        
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ScheduledTask>(), null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.Is<ScheduledTask>(t => t.Status == TaskStatus.Enabled), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerTaskAsync_ExistingTask_ShouldTriggerSuccessfully()
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

        var execution = new TaskExecution
        {
            Id = "execution-id",
            TaskId = taskId,
            Status = TaskStatus.Running
        };

        _mockCacheService.Setup(x => x.GetAsync<ScheduledTask>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(false);

        _mockTaskExecutor.Setup(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(execution);

        // Act
        var result = await _service.TriggerTaskAsync(taskId);

        // Assert
        Assert.Equal(execution.Id, result);
        _mockTaskExecutor.Verify(x => x.ExecuteAsync(task, It.IsAny<CancellationToken>()), Times.Once);
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
        _mockCacheService.Verify(x => x.SetAsync(taskKey, It.IsAny<ScheduledTask>(), null, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify index was updated with new task ID
        _mockCacheService.Verify(x => x.SetAsync(indexKey, 
            It.Is<List<string>>(list => list.Contains(result.Id) && list.Contains("existing-task")), 
            null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
