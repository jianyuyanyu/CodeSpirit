using CodeSpirit.Core;
using CodeSpirit.ScheduledTasks.Configuration;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskStatus = CodeSpirit.ScheduledTasks.Models.TaskStatus;

namespace CodeSpirit.ScheduledTasks.Controllers;

/// <summary>
/// 定时任务执行控制器
/// 提供统一的任务执行端点，供Web UI或其他服务调用
/// </summary>
[ApiController]
[Route("api/scheduled-tasks")]
[Authorize] // 使用 JWT 认证
public class ScheduledTaskExecutionController : ControllerBase
{
    private readonly IScheduledTaskService _taskService;
    private readonly ITaskExecutor _taskExecutor;
    private readonly ITaskHandlerRegistry _registry;
    private readonly ScheduledTasksOptions _options;
    private readonly ILogger<ScheduledTaskExecutionController> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ScheduledTaskExecutionController(
        IScheduledTaskService taskService,
        ITaskExecutor taskExecutor,
        ITaskHandlerRegistry registry,
        IOptions<ScheduledTasksOptions> options,
        ILogger<ScheduledTaskExecutionController> logger)
    {
        _taskService = taskService;
        _taskExecutor = taskExecutor;
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// 执行指定任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    [HttpPost("execute/{taskId}")]
    public async Task<ActionResult<ApiResponse>> ExecuteTask(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 获取任务信息
            var task = await _taskService.GetTaskAsync(taskId, cancellationToken);
            if (task == null)
            {
                return NotFound(ApiResponse.Error(404, $"任务不存在: {taskId}"));
            }
            
            // 2. 验证任务属于当前服务
            if (string.IsNullOrEmpty(_options.ServiceName))
            {
                return BadRequest(ApiResponse.Error(400, "服务名称未配置"));
            }
            
            var isOwned = await _registry.IsTaskOwnedByServiceAsync(taskId, _options.ServiceName, cancellationToken);
            if (!isOwned)
            {
                _logger.LogWarning("任务不属于当前服务 - TaskId: {TaskId}, CurrentService: {ServiceName}", 
                    taskId, _options.ServiceName);
                return BadRequest(ApiResponse.Error(400, $"任务不属于当前服务: {_options.ServiceName}"));
            }
            
            // 3. 检查任务是否正在执行
            if (await _taskExecutor.IsTaskRunningAsync(taskId))
            {
                return BadRequest(ApiResponse.Error(400, "任务正在执行中，无法重复触发"));
            }
            
            // 4. 执行任务（异步执行，不等待完成）
            _ = Task.Run(async () =>
            {
                try
                {
                    var execution = await _taskExecutor.ExecuteAsync(task, cancellationToken);
                    _logger.LogInformation("任务执行完成 - TaskId: {TaskId}, ExecutionId: {ExecutionId}, Status: {Status}", 
                        taskId, execution.Id, execution.Status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "任务执行异常 - TaskId: {TaskId}", taskId);
                }
            }, cancellationToken);
            
            _logger.LogInformation("✅ 任务触发成功 - TaskId: {TaskId}, ServiceName: {ServiceName}, UserId: {UserId}", 
                taskId, _options.ServiceName, User.FindFirst("id")?.Value ?? "unknown");
            
            return Ok(ApiResponse<object>.Success(new { taskId, message = "任务已触发执行" }, "任务触发成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发任务执行失败 - TaskId: {TaskId}", taskId);
            return StatusCode(500, ApiResponse.Error(500, $"任务触发失败: {ex.Message}"));
        }
    }
}

