using CodeSpirit.Core;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Controllers;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Tests;

/// <summary>
/// 定时任务执行控制器测试
/// </summary>
public class ScheduledTaskExecutionControllerTests
{
    private readonly Mock<IScheduledTaskService> _mockTaskService;
    private readonly Mock<ITaskExecutor> _mockTaskExecutor;
    private readonly Mock<ITaskHandlerRegistry> _mockRegistry;
    private readonly Mock<ILogger<ScheduledTaskExecutionController>> _mockLogger;
    private readonly ScheduledTasksOptions _options;
    private readonly ScheduledTaskExecutionController _controller;

    public ScheduledTaskExecutionControllerTests()
    {
        _mockTaskService = new Mock<IScheduledTaskService>();
        _mockTaskExecutor = new Mock<ITaskExecutor>();
        _mockRegistry = new Mock<ITaskHandlerRegistry>();
        _mockLogger = new Mock<ILogger<ScheduledTaskExecutionController>>();

        _options = new ScheduledTasksOptions
        {
            ServiceName = "test-service"
        };

        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _controller = new ScheduledTaskExecutionController(
            _mockTaskService.Object,
            _mockTaskExecutor.Object,
            _mockRegistry.Object,
            optionsMock.Object,
            _mockLogger.Object);

        // 设置 HttpContext 和用户认证
        var claims = new List<Claim> { new Claim("id", "test-user-id") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task ExecuteTask_TaskNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var taskId = "non-existing-task";
        _mockTaskService.Setup(x => x.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScheduledTask?)null);

        // Act
        var result = await _controller.ExecuteTask(taskId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.NotEqual(0, response.Status);
    }

    [Fact]
    public async Task ExecuteTask_TaskNotOwnedByService_ShouldReturnBadRequest()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "Test Task",
            HandlerType = "TestHandler"
        };

        _mockTaskService.Setup(x => x.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _mockRegistry.Setup(x => x.IsTaskOwnedByServiceAsync(taskId, _options.ServiceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExecuteTask(taskId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.NotEqual(0, response.Status);
    }

    [Fact]
    public async Task ExecuteTask_TaskAlreadyRunning_ShouldReturnBadRequest()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "Test Task",
            HandlerType = "TestHandler"
        };

        _mockTaskService.Setup(x => x.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _mockRegistry.Setup(x => x.IsTaskOwnedByServiceAsync(taskId, _options.ServiceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ExecuteTask(taskId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.NotEqual(0, response.Status);
    }

    [Fact]
    public async Task ExecuteTask_ValidTask_ShouldTriggerExecution()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "Test Task",
            HandlerType = "TestHandler"
        };

        _mockTaskService.Setup(x => x.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _mockRegistry.Setup(x => x.IsTaskOwnedByServiceAsync(taskId, _options.ServiceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockTaskExecutor.Setup(x => x.IsTaskRunningAsync(taskId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExecuteTask(taskId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        // 注意：控制器返回的是 ApiResponse<object>，因为 Success 方法需要泛型类型
        var response = Assert.IsAssignableFrom<ApiResponse<object>>(okResult.Value);
        Assert.Equal(0, response.Status);
        
        // 验证任务执行器被调用（异步执行）
        await Task.Delay(100); // 等待异步任务启动
        // 注意：由于是异步执行，这里只验证了控制器返回成功，实际执行是后台进行的
    }

    [Fact]
    public async Task ExecuteTask_ServiceNameNotConfigured_ShouldReturnBadRequest()
    {
        // Arrange
        var taskId = "test-task-id";
        var task = new ScheduledTask
        {
            Id = taskId,
            Name = "Test Task",
            HandlerType = "TestHandler"
        };

        var optionsWithoutServiceName = new ScheduledTasksOptions
        {
            ServiceName = string.Empty
        };
        var optionsMock = new Mock<IOptions<ScheduledTasksOptions>>();
        optionsMock.Setup(x => x.Value).Returns(optionsWithoutServiceName);

        var controller = new ScheduledTaskExecutionController(
            _mockTaskService.Object,
            _mockTaskExecutor.Object,
            _mockRegistry.Object,
            optionsMock.Object,
            _mockLogger.Object);

        controller.ControllerContext = _controller.ControllerContext;

        _mockTaskService.Setup(x => x.GetTaskAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await controller.ExecuteTask(taskId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.NotEqual(0, response.Status);
    }
}

