using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers;

/// <summary>
/// 定时任务管理控制器
/// </summary>
[DisplayName("定时任务")]
[Navigation(Icon = "fa-solid fa-clock", PlatformType = PlatformType.Tenant)]
public class ScheduledTasksController : ApiControllerBase
{
    private readonly IScheduledTaskService _taskService;
    private readonly IScheduledTaskQueryService _queryService;
    private readonly ITaskExecutor _taskExecutor;
    private readonly ILogger<ScheduledTasksController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="taskService">任务服务</param>
    /// <param name="queryService">查询服务</param>
    /// <param name="taskExecutor">任务执行器</param>
    /// <param name="logger">日志记录器</param>
    public ScheduledTasksController(
        IScheduledTaskService taskService,
        IScheduledTaskQueryService queryService,
        ITaskExecutor taskExecutor,
        ILogger<ScheduledTasksController> logger)
    {
        _taskService = taskService;
        _queryService = queryService;
        _taskExecutor = taskExecutor;
        _logger = logger;
    }

    /// <summary>
    /// 获取定时任务列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>任务列表</returns>
    [HttpGet]
    [DisplayName("获取任务列表")]
    public async Task<ActionResult<ApiResponse<PageList<ScheduledTask>>>> GetTasks([FromQuery] TaskQueryDto queryDto)
    {
        var result = await _queryService.GetTasksPagedAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取定时任务详情
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>任务详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取任务详情")]
    public async Task<ActionResult<ApiResponse>> GetTask(string id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        return Ok(ApiResponse<object>.Success(task));
    }

    /// <summary>
    /// 创建定时任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建任务")]
    public async Task<ActionResult<ApiResponse>> CreateTask([FromBody] ScheduledTask task)
    {
        try
        {
            var createdTask = await _taskService.CreateTaskAsync(task);
            return Ok(ApiResponse<object>.Success(createdTask, "任务创建成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error(400, ex.Message));
        }
    }

    /// <summary>
    /// 更新定时任务
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <param name="task">任务信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新任务")]
    public async Task<ActionResult<ApiResponse>> UpdateTask(string id, [FromBody] ScheduledTask task)
    {
        if (id != task.Id)
        {
            return BadRequest(ApiResponse.Error(400, "任务ID不匹配"));
        }

        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(task);
            if (updatedTask == null)
            {
                return NotFound(ApiResponse.Error(404, "任务不存在"));
            }

            return Ok(ApiResponse<object>.Success(updatedTask, "任务更新成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error(400, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error(400, ex.Message));
        }
    }

    /// <summary>
    /// 删除定时任务
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除任务")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(string id)
    {
        try
        {
            var success = await _taskService.DeleteTaskAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse.Error(404, "任务不存在"));
            }

            return Ok(ApiResponse.Success("任务删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error(400, ex.Message));
        }
    }

    /// <summary>
    /// 启用定时任务
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>启用结果</returns>
    [HttpPut("{id}/enable")]
    [Operation("启用", "ajax", null, "确定要启用此任务吗？", "status != 1")]
    [DisplayName("启用任务")]
    public async Task<ActionResult<ApiResponse>> EnableTask(string id)
    {
        var success = await _taskService.EnableTaskAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        return Ok(ApiResponse.Success("任务启用成功"));
    }

    /// <summary>
    /// 禁用定时任务
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>禁用结果</returns>
    [HttpPut("{id}/disable")]
    [Operation("禁用", "ajax", null, "确定要禁用此任务吗？", "status == 1")]
    [DisplayName("禁用任务")]
    public async Task<ActionResult<ApiResponse>> DisableTask(string id)
    {
        var success = await _taskService.DisableTaskAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        return Ok(ApiResponse.Success("任务禁用成功"));
    }

    /// <summary>
    /// 手动触发任务执行
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>触发结果</returns>
    [HttpPost("{id}/trigger")]
    [Operation("立即执行", "ajax", null, "确定要立即执行此任务吗？")]
    [DisplayName("触发执行")]
    public async Task<ActionResult<ApiResponse>> TriggerTask(string id)
    {
        try
        {
            var executionId = await _taskService.TriggerTaskAsync(id);
            return Ok(ApiResponse<object>.Success(new { executionId }, "任务触发成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error(400, ex.Message));
        }
    }

    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <returns>取消结果</returns>
    [HttpPost("executions/{executionId}/cancel")]
    [Operation("取消执行", "ajax", null, "确定要取消此任务执行吗？")]
    [DisplayName("取消执行")]
    public async Task<ActionResult<ApiResponse>> CancelExecution(string executionId)
    {
        var success = await _taskService.CancelExecutionAsync(executionId);
        if (!success)
        {
            return NotFound(ApiResponse.Error(404, "执行记录不存在或已完成"));
        }

        return Ok(ApiResponse.Success("任务执行已取消"));
    }

    /// <summary>
    /// 获取任务执行历史
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <param name="queryDto">查询参数</param>
    /// <returns>执行历史</returns>
    [HttpGet("{id}/executions")]
    [DisplayName("获取执行历史")]
    public async Task<ActionResult<ApiResponse>> GetTaskExecutions(string id, [FromQuery] CodeSpirit.Core.Dtos.QueryDtoBase queryDto)
    {
        var result = await _queryService.GetExecutionHistoryAsync(id, queryDto);
        return Ok(ApiResponse<object>.Success(result));
    }

    /// <summary>
    /// 获取所有执行历史
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>执行历史</returns>
    [HttpGet("executions")]
    [DisplayName("获取所有执行历史")]
    public async Task<ActionResult<ApiResponse>> GetAllExecutions([FromQuery] ExecutionQueryDto queryDto)
    {
        var result = await _queryService.GetAllExecutionHistoryAsync(queryDto);
        return Ok(ApiResponse<object>.Success(result));
    }

    /// <summary>
    /// 获取正在执行的任务
    /// </summary>
    /// <returns>执行中的任务</returns>
    [HttpGet("running")]
    [DisplayName("获取运行中任务")]
    public async Task<ActionResult<ApiResponse>> GetRunningTasks()
    {
        var runningTasks = await _queryService.GetRunningExecutionsAsync();
        return Ok(ApiResponse<object>.Success(runningTasks));
    }

    /// <summary>
    /// 获取任务统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取统计信息")]
    public async Task<ActionResult<ApiResponse>> GetStatistics()
    {
        var statistics = await _queryService.GetTaskStatisticsAsync();
        return Ok(ApiResponse<object>.Success(statistics));
    }

    /// <summary>
    /// 重新加载配置文件任务
    /// </summary>
    /// <returns>加载结果</returns>
    [HttpPost("reload-config")]
    [Operation("重新加载配置", "ajax", null, "确定要重新加载配置文件中的任务吗？")]
    [DisplayName("重新加载配置")]
    public async Task<ActionResult<ApiResponse>> ReloadConfigTasks()
    {
        var loadedCount = await _taskService.LoadTasksFromConfigurationAsync();
        return Ok(ApiResponse<object>.Success(new { loadedCount }, $"成功加载 {loadedCount} 个配置任务"));
    }

    /// <summary>
    /// 验证Cron表达式
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-cron")]
    [DisplayName("验证Cron表达式")]
    public async Task<ActionResult<ApiResponse>> ValidateCron([FromBody] ValidateCronRequest request)
    {
        var isValid = CodeSpirit.ScheduledTasks.Helpers.CronHelper.IsValidCronExpression(request.CronExpression);
        
        if (!isValid)
        {
            return Ok(ApiResponse<object>.Success(new { isValid = false, message = "无效的Cron表达式" }));
        }

        var nextExecutions = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetNextOccurrences(request.CronExpression, 5);
        var description = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetDescription(request.CronExpression);

        return await Task.FromResult(Ok(ApiResponse<object>.Success(new 
        { 
            isValid = true, 
            description,
            nextExecutions 
        })));
    }

    /// <summary>
    /// 获取Cron表达式预设
    /// </summary>
    /// <returns>预设列表</returns>
    [HttpGet("cron-presets")]
    [DisplayName("获取Cron预设")]
    public async Task<ActionResult<ApiResponse>> GetCronPresets()
    {
        var presets = CodeSpirit.ScheduledTasks.Helpers.CronHelper.Presets.GetAll();
        return await Task.FromResult(Ok(ApiResponse<object>.Success(presets)));
    }
}

/// <summary>
/// 验证Cron表达式请求
/// </summary>
public class ValidateCronRequest
{
    /// <summary>
    /// Cron表达式
    /// </summary>
    [DisplayName("Cron表达式")]
    [Required(ErrorMessage = "Cron表达式不能为空")]
    public string CronExpression { get; set; } = string.Empty;
}
