using CodeSpirit.Core.Enums;
using CodeSpirit.PathfinderApi.Dtos.Task;
using CodeSpirit.PathfinderApi.Services.Interfaces;

namespace CodeSpirit.PathfinderApi.Controllers;

/// <summary>
/// 任务管理控制器
/// </summary>
[DisplayName("任务管理")]
[Navigation(Icon = "fa-solid fa-tasks", PlatformType = PlatformType.Tenant)]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public TasksController(
        ITaskService taskService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取任务列表
    /// </summary>
    [HttpGet]
    [DisplayName("获取任务列表")]
    public async Task<ActionResult<ApiResponse<PageList<TaskDto>>>> GetTasks([FromQuery] TaskQueryDto queryDto)
    {
        var tasks = await _taskService.GetTasksAsync(queryDto);
        return SuccessResponse(tasks);
    }
    
    /// <summary>
    /// 获取任务详情
    /// </summary>
    [HttpGet("{id}")]
    [DisplayName("获取任务详情")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> Detail(Guid id)
    {
        var task = await _taskService.GetAsync(id);
        if (task == null)
        {
            return BadResponse<TaskDto>("任务不存在");
        }
        return SuccessResponse(task);
    }
    
    /// <summary>
    /// 创建任务
    /// </summary>
    [HttpPost]
    [DisplayName("创建任务")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(CreateTaskDto dto)
    {
        try
        {
            var createdTask = await _taskService.CreateAsync(dto);
            return SuccessResponseWithCreate<TaskDto>(nameof(Detail), createdTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建任务失败");
            return BadResponse<TaskDto>("创建任务失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 更新任务
    /// </summary>
    [HttpPut("{id}")]
    [DisplayName("更新任务")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, UpdateTaskDto dto)
    {
        try
        {
            await _taskService.UpdateAsync(id, dto);
            var updatedTask = await _taskService.GetAsync(id);
            return SuccessResponse(updatedTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务失败: {TaskId}", id);
            return BadResponse<TaskDto>("更新任务失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 删除任务
    /// </summary>
    [HttpDelete("{id}")]
    [DisplayName("删除任务")]
    public async Task<ActionResult<ApiResponse>> DeleteTask(Guid id)
    {
        try
        {
            await _taskService.DeleteAsync(id);
            return SuccessResponse("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除任务失败: {TaskId}", id);
            return BadResponse("删除任务失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 批量创建任务
    /// </summary>
    /// <param name="request">批量创建请求</param>
    /// <returns>创建的任务列表</returns>
    [HttpPost("batch")]
    [DisplayName("批量创建任务")]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> BatchCreateTasks(BatchCreateTasksRequest request)
    {
        try
        {
            _logger.LogInformation("开始批量创建任务: GoalId={GoalId}, TaskCount={TaskCount}", 
                request.GoalId, request.Tasks.Count);
            
            var createdTasks = await _taskService.BatchCreateTasksAsync(request);
            
            _logger.LogInformation("批量创建任务成功: GoalId={GoalId}, CreatedCount={CreatedCount}", 
                request.GoalId, createdTasks.Count);
            
            return SuccessResponse(createdTasks, $"成功创建 {createdTasks.Count} 个任务");
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "批量创建任务失败（业务异常）: GoalId={GoalId}", request.GoalId);
            return BadResponse<List<TaskDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量创建任务失败: GoalId={GoalId}", request.GoalId);
            return BadResponse<List<TaskDto>>("批量创建任务失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 更新任务状态
    /// </summary>
    [HttpPut("{id}/status")]
    [DisplayName("更新任务状态")]
    [Operation("更新状态", "form")]
    public async Task<ActionResult<ApiResponse>> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
    {
        try
        {
            var success = await _taskService.UpdateTaskStatusAsync(id, dto.Status);
            if (!success)
            {
                return BadResponse("任务不存在");
            }
            
            return SuccessResponse("状态更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务状态失败: {TaskId}", id);
            return BadResponse("更新状态失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 获取任务依赖
    /// </summary>
    [HttpGet("{id}/dependencies")]
    [DisplayName("获取任务依赖")]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> GetTaskDependencies(Guid id)
    {
        try
        {
            var dependencies = await _taskService.GetTaskDependenciesAsync(id);
            return SuccessResponse(dependencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务依赖失败: {TaskId}", id);
            return BadResponse<List<TaskDto>>("获取任务依赖失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 导出任务列表
    /// </summary>
    [HttpGet("Export")]
    [DisplayName("导出任务列表")]
    public async Task<ActionResult<ApiResponse<PageList<TaskDto>>>> Export([FromQuery] TaskQueryDto queryDto)
    {
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;
        
        var tasks = await _taskService.GetTasksAsync(queryDto);
        
        if (tasks.Items.Count == 0)
        {
            return BadResponse<PageList<TaskDto>>("没有数据可供导出");
        }
        
        return SuccessResponse(tasks);
    }
}

/// <summary>
/// 更新任务状态DTO
/// </summary>
public class UpdateTaskStatusDto
{
    /// <summary>
    /// 任务状态
    /// </summary>
    [Required(ErrorMessage = "任务状态不能为空")]
    [DisplayName("任务状态")]
    public Models.Enums.TaskStatus Status { get; set; }
}

