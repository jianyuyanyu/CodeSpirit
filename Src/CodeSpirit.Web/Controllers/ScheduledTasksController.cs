using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Net.Http.Headers;
using AutoMapper;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.ScheduledTasks.Models;
using CodeSpirit.ScheduledTasks.Services;
using CodeSpirit.ScheduledTasks.Dto;
using CodeSpirit.Web.Configuration.Statistics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Web.Controllers;

/// <summary>
/// 定时任务管理控制器
/// </summary>
[DisplayName("定时任务")]
[Navigation(Icon = "fa-solid fa-clock", PlatformType = PlatformType.System)]
[StatisticsCards<ScheduledTaskStatisticsConfig>]
public class ScheduledTasksController : ApiControllerBase
{
    private readonly IScheduledTaskService _taskService;
    private readonly IScheduledTaskQueryService _queryService;
    private readonly ITaskExecutor _taskExecutor;
    private readonly ITaskHandlerRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ScheduledTasksController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="taskService">任务服务</param>
    /// <param name="queryService">查询服务</param>
    /// <param name="taskExecutor">任务执行器</param>
    /// <param name="registry">任务注册表</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    /// <param name="mapper">对象映射器</param>
    /// <param name="logger">日志记录器</param>
    public ScheduledTasksController(
        IScheduledTaskService taskService,
        IScheduledTaskQueryService queryService,
        ITaskExecutor taskExecutor,
        ITaskHandlerRegistry registry,
        IHttpClientFactory httpClientFactory,
        IMapper mapper,
        ILogger<ScheduledTasksController> logger)
    {
        _taskService = taskService;
        _queryService = queryService;
        _taskExecutor = taskExecutor;
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 获取定时任务列表
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <returns>任务列表</returns>
    [HttpGet]
    [DisplayName("获取任务列表")]
    public async Task<ActionResult<ApiResponse<PageList<ScheduledTaskDto>>>> GetTasks([FromQuery] ScheduledTaskQueryDto queryDto)
    {
        // 转换为内部查询 DTO
        var internalQuery = new TaskQueryDto
        {
            Page = queryDto.Page,
            PerPage = queryDto.PerPage,
            Keywords = queryDto.Keywords,
            OrderBy = queryDto.OrderBy,
            OrderDir = queryDto.OrderDir,
            Type = queryDto.Type,
            Status = queryDto.Status,
            Group = queryDto.Group,
            HandlerType = queryDto.HandlerType
        };
        
        var result = await _queryService.GetTasksPagedAsync(internalQuery);
        var dtoList = _mapper.Map<List<ScheduledTaskDto>>(result.Items);
        var pagedResult = new PageList<ScheduledTaskDto>(dtoList, result.Total);
        return SuccessResponse(pagedResult);
    }

    /// <summary>
    /// 获取定时任务详情
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>任务详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取任务详情")]
    public async Task<ActionResult<ApiResponse<ScheduledTaskDto>>> GetTask(string id)
    {
        var task = await _taskService.GetTaskAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        var dto = _mapper.Map<ScheduledTaskDto>(task);
        return SuccessResponse(dto);
    }

    #region 分组管理

    /// <summary>
    /// 获取所有任务分组
    /// </summary>
    /// <returns>分组列表</returns>
    [HttpGet("groups")]
    [DisplayName("获取任务分组")]
    public async Task<ActionResult<ApiResponse>> GetTaskGroups()
    {
        var allTasks = await _taskService.GetAllTasksAsync();
        
        var groups = allTasks
            .Where(t => !string.IsNullOrEmpty(t.Group))
            .GroupBy(t => t.Group!)
            .Select(g => new TaskGroupInfo
            {
                Name = g.Key,
                TaskCount = g.Count(),
                EnabledCount = g.Count(t => t.Status == CodeSpirit.ScheduledTasks.Models.TaskStatus.Enabled),
                DisabledCount = g.Count(t => t.Status == CodeSpirit.ScheduledTasks.Models.TaskStatus.Disabled)
            })
            .OrderBy(g => g.Name)
            .ToList();

        // 添加未分组的统计
        var ungroupedCount = allTasks.Count(t => string.IsNullOrEmpty(t.Group));
        if (ungroupedCount > 0)
        {
            groups.Insert(0, new TaskGroupInfo
            {
                Name = "(未分组)",
                TaskCount = ungroupedCount,
                EnabledCount = allTasks.Count(t => string.IsNullOrEmpty(t.Group) && 
                    t.Status == CodeSpirit.ScheduledTasks.Models.TaskStatus.Enabled),
                DisabledCount = allTasks.Count(t => string.IsNullOrEmpty(t.Group) && 
                    t.Status == CodeSpirit.ScheduledTasks.Models.TaskStatus.Disabled)
            });
        }

        return Ok(ApiResponse<object>.Success(new
        {
            Groups = groups,
            TotalGroups = groups.Count(g => g.Name != "(未分组)"),
            TotalTasks = allTasks.Count
        }));
    }

    /// <summary>
    /// 获取指定分组的任务列表
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <param name="queryDto">查询参数</param>
    /// <returns>任务列表</returns>
    [HttpGet("groups/{groupName}/tasks")]
    [DisplayName("获取分组任务")]
    public async Task<ActionResult<ApiResponse>> GetTasksByGroup(string groupName, [FromQuery] TaskQueryDto queryDto)
    {
        // 设置分组过滤条件
        queryDto.Group = groupName == "(未分组)" ? null : groupName;
        
        var result = await _queryService.GetTasksPagedAsync(queryDto);
        
        // 如果是未分组，需要特殊处理
        if (groupName == "(未分组)")
        {
            var allTasks = await _taskService.GetAllTasksAsync();
            var ungroupedTasks = allTasks
                .Where(t => string.IsNullOrEmpty(t.Group))
                .OrderBy(t => t.Name)
                .Skip((queryDto.Page - 1) * queryDto.PerPage)
                .Take(queryDto.PerPage)
                .ToList();
            
            var totalUngrouped = allTasks.Count(t => string.IsNullOrEmpty(t.Group));
            return Ok(ApiResponse<object>.Success(new PageList<ScheduledTask>(ungroupedTasks, totalUngrouped)));
        }
        
        return Ok(ApiResponse<object>.Success(result));
    }

    /// <summary>
    /// 批量设置任务分组
    /// </summary>
    /// <param name="request">批量分组请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("groups/batch-set")]
    [DisplayName("批量设置分组")]
    public async Task<ActionResult<ApiResponse>> BatchSetTaskGroup([FromBody] BatchSetGroupRequest request)
    {
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(ApiResponse.Error(400, "请选择要操作的任务"));
        }

        var results = new BatchOperationResult();
        foreach (var taskId in request.TaskIds)
        {
            try
            {
                var task = await _taskService.GetTaskAsync(taskId);
                if (task == null)
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "任务不存在" });
                    continue;
                }

                task.Group = request.GroupName;
                var updated = await _taskService.UpdateTaskAsync(task);
                
                if (updated != null)
                {
                    results.SuccessCount++;
                    results.SuccessIds.Add(taskId);
                }
                else
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "更新失败" });
                }
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = ex.Message });
            }
        }

        return Ok(ApiResponse<object>.Success(results, $"批量设置分组完成：成功 {results.SuccessCount} 个，失败 {results.FailedCount} 个"));
    }

    #endregion

    /// <summary>
    /// 创建定时任务
    /// </summary>
    /// <param name="dto">任务信息</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [HeaderOperation("新增任务", OperationActionType.Form, Icon = "fa-solid fa-plus", DialogSize = DialogSize.LG)]
    [DisplayName("创建任务")]
    public async Task<ActionResult<ApiResponse<ScheduledTaskDto>>> CreateTask([FromBody] CreateScheduledTaskDto dto)
    {
        try
        {
            var task = _mapper.Map<ScheduledTask>(dto);
            task.Id = Guid.NewGuid().ToString();
            
            var createdTask = await _taskService.CreateTaskAsync(task);
            var resultDto = _mapper.Map<ScheduledTaskDto>(createdTask);
            return SuccessResponse(resultDto, "任务创建成功");
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
    /// <param name="dto">任务信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [Operation("编辑", OperationActionType.Form, Icon = "fa-solid fa-edit", DialogSize = DialogSize.LG)]
    [DisplayName("更新任务")]
    public async Task<ActionResult<ApiResponse<ScheduledTaskDto>>> UpdateTask(string id, [FromBody] UpdateScheduledTaskDto dto)
    {
        try
        {
            var existingTask = await _taskService.GetTaskAsync(id);
            if (existingTask == null)
            {
                return NotFound(ApiResponse.Error(404, "任务不存在"));
            }

            // 映射更新字段
            _mapper.Map(dto, existingTask);
            existingTask.UpdatedAt = DateTime.UtcNow;

            var updatedTask = await _taskService.UpdateTaskAsync(existingTask);
            if (updatedTask == null)
            {
                return NotFound(ApiResponse.Error(404, "任务不存在"));
            }

            var resultDto = _mapper.Map<ScheduledTaskDto>(updatedTask);
            return SuccessResponse(resultDto, "任务更新成功");
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
    [Operation("删除", OperationActionType.Ajax, confirmText: "确定要删除该任务吗？", visibleOn: "isFromConfiguration == false", Icon = "fa-solid fa-trash")]
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

            return SuccessResponse("任务删除成功");
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
    [Operation("启用", OperationActionType.Ajax, confirmText: "确定要启用此任务吗？", visibleOn: "status != 'Enabled' && status != 1", Icon = "fa-solid fa-play")]
    [DisplayName("启用任务")]
    public async Task<ActionResult<ApiResponse>> EnableTask(string id)
    {
        var success = await _taskService.EnableTaskAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        return SuccessResponse("任务启用成功");
    }

    /// <summary>
    /// 禁用定时任务
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>禁用结果</returns>
    [HttpPut("{id}/disable")]
    [Operation("禁用", OperationActionType.Ajax, confirmText: "确定要禁用此任务吗？", visibleOn: "status == 'Enabled' || status == 1", Icon = "fa-solid fa-pause")]
    [DisplayName("禁用任务")]
    public async Task<ActionResult<ApiResponse>> DisableTask(string id)
    {
        var success = await _taskService.DisableTaskAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Error(404, "任务不存在"));
        }

        return SuccessResponse("任务禁用成功");
    }

    /// <summary>
    /// 手动触发任务执行
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>触发结果</returns>
    [HttpPost("{id}/trigger")]
    [Operation("立即执行", OperationActionType.Ajax, confirmText: "确定要立即执行此任务吗？", Icon = "fa-solid fa-rocket")]
    [DisplayName("触发执行")]
    public async Task<ActionResult<ApiResponse>> TriggerTask(string id)
    {
        try
        {
            // 1. 查询任务所属服务
            var serviceName = await _registry.GetTaskServiceNameAsync(id);
            
            if (string.IsNullOrEmpty(serviceName))
            {
                _logger.LogWarning("无法确定任务所属服务 - TaskId: {TaskId}", id);
                return BadRequest(ApiResponse.Error(400, "无法确定任务所属服务"));
            }
            
            // 2. 获取当前用户的 JWT Token
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(ApiResponse.Error(401, "未提供有效的认证令牌"));
            }
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // 3. 构建执行端点 URL（通过 Aspire 服务发现）
            var serviceUrl = $"http://{serviceName}";
            var executeUrl = $"{serviceUrl}/api/scheduled-tasks/execute/{id}";
            
            _logger.LogInformation("🚀 触发任务执行 - TaskId: {TaskId}, ServiceName: {ServiceName}, Url: {Url}, UserId: {UserId}", 
                id, serviceName, executeUrl, User.FindFirst("id")?.Value ?? "unknown");
            
            // 4. 使用 HttpClient 调用（传递 JWT Token）
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            
            var response = await httpClient.PostAsync(executeUrl, null);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // 5. 返回结果
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ 任务触发成功 - TaskId: {TaskId}, ServiceName: {ServiceName}, UserId: {UserId}", 
                    id, serviceName, User.FindFirst("id")?.Value ?? "unknown");
                return Ok(ApiResponse.Success("任务触发成功"));
            }
            else
            {
                _logger.LogWarning("❌ 任务触发失败 - TaskId: {TaskId}, ServiceName: {ServiceName}, StatusCode: {StatusCode}, Response: {Response}, UserId: {UserId}", 
                    id, serviceName, response.StatusCode, responseContent, User.FindFirst("id")?.Value ?? "unknown");
                return StatusCode((int)response.StatusCode, 
                    ApiResponse.Error((int)response.StatusCode, $"任务触发失败: {responseContent}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发任务执行异常 - TaskId: {TaskId}", id);
            return StatusCode(500, ApiResponse.Error(500, $"任务触发失败: {ex.Message}"));
        }
    }

    ///// <summary>
    ///// 取消任务执行
    ///// </summary>
    ///// <param name="executionId">执行ID</param>
    ///// <returns>取消结果</returns>
    //[HttpPost("executions/{executionId}/cancel")]
    //[Operation("取消执行", "ajax", null, "确定要取消此任务执行吗？", Icon = "fa-solid fa-pause")]
    //[DisplayName("取消执行")]
    //public async Task<ActionResult<ApiResponse>> CancelExecution(string executionId)
    //{
    //    var success = await _taskService.CancelExecutionAsync(executionId);
    //    if (!success)
    //    {
    //        return NotFound(ApiResponse.Error(404, "执行记录不存在或已完成"));
    //    }

    //    return Ok(ApiResponse.Success("任务执行已取消"));
    //}

    /// <summary>
    /// 获取任务执行历史（弹窗 Schema）
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <returns>执行历史弹窗 Schema</returns>
    [HttpGet("{id}/executions")]
    [CrudDialogOperation("执行历史",
        DataApi = "/api/web/ScheduledTasks/${id}/executions/data",
        DataType = typeof(TaskExecution),
        Icon = "fa-solid fa-clock-rotate-left",
        DialogSize = DialogSize.XL,
        EnableRefresh = true,
        PerPage = 10)]
    [DisplayName("获取执行历史")]
    public ActionResult<ApiResponse> GetTaskExecutions(string id)
    {
        //TODO:后续通过Dto方法简化RowActions的定义
        // 使用基类封装的通用方法生成 schema
        return GenerateCrudDialogSchema(new Dictionary<string, string> { { "id", id } });
    }

    /// <summary>
    /// 获取任务执行历史数据
    /// </summary>
    /// <param name="id">任务ID</param>
    /// <param name="queryDto">查询参数</param>
    /// <returns>执行历史数据</returns>
    [HttpGet("{id}/executions/data")]
    [DisplayName("获取执行历史数据")]
    public async Task<ActionResult<ApiResponse>> GetTaskExecutionsData(string id, [FromQuery] CodeSpirit.Core.Dtos.QueryDtoBase queryDto)
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
    public async Task<ActionResult<ApiResponse<PageList<TaskExecutionDto>>>> GetAllExecutions([FromQuery] TaskExecutionQueryDto queryDto)
    {
        // 转换为内部查询 DTO
        var internalQuery = new ExecutionQueryDto
        {
            Page = queryDto.Page,
            PerPage = queryDto.PerPage,
            Keywords = queryDto.Keywords,
            OrderBy = queryDto.OrderBy,
            OrderDir = queryDto.OrderDir,
            TaskId = queryDto.TaskId,
            Status = queryDto.Status,
            TriggerType = queryDto.TriggerType,
            ExecutionNode = queryDto.ExecutionNode
        };
        
        var result = await _queryService.GetAllExecutionHistoryAsync(internalQuery);
        var dtoList = _mapper.Map<List<TaskExecutionDto>>(result.Items);
        var pagedResult = new PageList<TaskExecutionDto>(dtoList, result.Total);
        return SuccessResponse(pagedResult);
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
    /// 获取统计卡片数据
    /// </summary>
    /// <returns>统计卡片数据</returns>
    [HttpGet("statistics/cards")]
    [DisplayName("获取统计卡片")]
    public async Task<ActionResult<ApiResponse>> GetStatisticsCards()
    {
        var stats = await _queryService.GetTaskStatisticsAsync();
        
        // 返回扁平化数据供卡片渲染
        var data = new
        {
            todayExecutions = stats.TodayExecutions,
            todaySuccessExecutions = stats.TodaySuccessExecutions,
            todayFailedExecutions = stats.TodayFailedExecutions,
            successRate = $"{stats.SuccessRate:F1}%"
        };
        
        return Ok(ApiResponse<object>.Success(data));
    }

    /// <summary>
    /// 获取仪表板数据
    /// </summary>
    /// <param name="days">趋势数据天数（默认7天）</param>
    /// <returns>仪表板数据</returns>
    [HttpGet("dashboard")]
    [DisplayName("获取仪表板数据")]
    public async Task<ActionResult<ApiResponse>> GetDashboard([FromQuery] int days = 7)
    {
        var dashboard = await _queryService.GetDashboardDataAsync(days);
        return Ok(ApiResponse<object>.Success(dashboard));
    }

    ///// <summary>
    ///// 重新加载配置文件任务
    ///// </summary>
    ///// <returns>加载结果</returns>
    //[HttpPost("reload-config")]
    //[HeaderOperation("重新加载配置", OperationActionType.Ajax, confirmText: "确定要重新加载配置文件中的任务吗？", Icon = "fa-solid fa-refresh")]
    //[DisplayName("重新加载配置")]
    //public async Task<ActionResult<ApiResponse>> ReloadConfigTasks()
    //{
    //    var loadedCount = await _taskService.LoadTasksFromConfigurationAsync();
    //    return Ok(ApiResponse<object>.Success(new { loadedCount }, $"成功加载 {loadedCount} 个配置任务"));
    //}

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

    /// <summary>
    /// 构建Cron表达式
    /// </summary>
    /// <param name="request">构建请求</param>
    /// <returns>构建结果</returns>
    [HttpPost("build-cron")]
    [DisplayName("构建Cron表达式")]
    public async Task<ActionResult<ApiResponse>> BuildCron([FromBody] CronBuilderRequest request)
    {
        try
        {
            // 构建 Cron 表达式
            var cronExpression = $"{request.Second} {request.Minute} {request.Hour} {request.DayOfMonth} {request.Month} {request.DayOfWeek}";
            
            // 验证表达式
            var isValid = CodeSpirit.ScheduledTasks.Helpers.CronHelper.IsValidCronExpression(cronExpression);
            
            if (!isValid)
            {
                return Ok(ApiResponse<object>.Success(new
                {
                    isValid = false,
                    expression = cronExpression,
                    message = "生成的Cron表达式无效，请检查各字段的值"
                }));
            }

            // 获取描述和下次执行时间
            var description = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetDescription(cronExpression);
            var nextExecutions = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetNextOccurrences(cronExpression, 5);

            return await Task.FromResult(Ok(ApiResponse<object>.Success(new
            {
                isValid = true,
                expression = cronExpression,
                description,
                nextExecutions
            })));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Error(400, $"构建Cron表达式失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 解析Cron表达式为各字段
    /// </summary>
    /// <param name="cronExpression">Cron表达式</param>
    /// <returns>解析结果</returns>
    [HttpGet("parse-cron")]
    [DisplayName("解析Cron表达式")]
    public async Task<ActionResult<ApiResponse>> ParseCron([FromQuery] string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return BadRequest(ApiResponse.Error(400, "Cron表达式不能为空"));
        }

        var isValid = CodeSpirit.ScheduledTasks.Helpers.CronHelper.IsValidCronExpression(cronExpression);
        
        if (!isValid)
        {
            return Ok(ApiResponse<object>.Success(new
            {
                isValid = false,
                message = "无效的Cron表达式"
            }));
        }

        // 解析各字段
        var parts = cronExpression.Split(' ');
        if (parts.Length != 6)
        {
            return Ok(ApiResponse<object>.Success(new
            {
                isValid = false,
                message = "Cron表达式格式不正确，应包含6个字段（秒 分 时 日 月 周）"
            }));
        }

        var description = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetDescription(cronExpression);
        var nextExecutions = CodeSpirit.ScheduledTasks.Helpers.CronHelper.GetNextOccurrences(cronExpression, 5);

        return await Task.FromResult(Ok(ApiResponse<object>.Success(new
        {
            isValid = true,
            expression = cronExpression,
            fields = new CronBuilderRequest
            {
                Second = parts[0],
                Minute = parts[1],
                Hour = parts[2],
                DayOfMonth = parts[3],
                Month = parts[4],
                DayOfWeek = parts[5]
            },
            description,
            nextExecutions
        })));
    }

    /// <summary>
    /// 获取Cron编辑器选项
    /// </summary>
    /// <returns>编辑器选项</returns>
    [HttpGet("cron-editor-options")]
    [DisplayName("获取Cron编辑器选项")]
    public async Task<ActionResult<ApiResponse>> GetCronEditorOptions()
    {
        var options = new CronEditorOptions
        {
            Seconds = GenerateOptions(0, 59, "秒"),
            Minutes = GenerateOptions(0, 59, "分钟"),
            Hours = GenerateOptions(0, 23, "小时"),
            DaysOfMonth = GenerateOptions(1, 31, "日"),
            Months = new List<CronOption>
            {
                new() { Value = "*", Label = "每月" },
                new() { Value = "1", Label = "一月" },
                new() { Value = "2", Label = "二月" },
                new() { Value = "3", Label = "三月" },
                new() { Value = "4", Label = "四月" },
                new() { Value = "5", Label = "五月" },
                new() { Value = "6", Label = "六月" },
                new() { Value = "7", Label = "七月" },
                new() { Value = "8", Label = "八月" },
                new() { Value = "9", Label = "九月" },
                new() { Value = "10", Label = "十月" },
                new() { Value = "11", Label = "十一月" },
                new() { Value = "12", Label = "十二月" }
            },
            DaysOfWeek = new List<CronOption>
            {
                new() { Value = "*", Label = "每天" },
                new() { Value = "?", Label = "不指定" },
                new() { Value = "0", Label = "周日" },
                new() { Value = "1", Label = "周一" },
                new() { Value = "2", Label = "周二" },
                new() { Value = "3", Label = "周三" },
                new() { Value = "4", Label = "周四" },
                new() { Value = "5", Label = "周五" },
                new() { Value = "6", Label = "周六" },
                new() { Value = "1-5", Label = "工作日" },
                new() { Value = "0,6", Label = "周末" }
            },
            CommonPatterns = new List<CronPattern>
            {
                new() { Name = "每秒执行", Expression = "* * * * * *" },
                new() { Name = "每分钟执行", Expression = "0 * * * * *" },
                new() { Name = "每5分钟执行", Expression = "0 */5 * * * *" },
                new() { Name = "每15分钟执行", Expression = "0 */15 * * * *" },
                new() { Name = "每30分钟执行", Expression = "0 */30 * * * *" },
                new() { Name = "每小时执行", Expression = "0 0 * * * *" },
                new() { Name = "每天0点执行", Expression = "0 0 0 * * *" },
                new() { Name = "每天上午9点", Expression = "0 0 9 * * *" },
                new() { Name = "每天下午6点", Expression = "0 0 18 * * *" },
                new() { Name = "工作日上午9点", Expression = "0 0 9 * * 1-5" },
                new() { Name = "每周一0点", Expression = "0 0 0 * * 1" },
                new() { Name = "每月1号0点", Expression = "0 0 0 1 * *" }
            }
        };

        return await Task.FromResult(Ok(ApiResponse<object>.Success(options)));
    }

    private static List<CronOption> GenerateOptions(int start, int end, string unit)
    {
        var options = new List<CronOption>
        {
            new() { Value = "*", Label = $"每{unit}" }
        };

        for (int i = start; i <= end; i++)
        {
            options.Add(new CronOption { Value = i.ToString(), Label = i.ToString() });
        }

        return options;
    }

    #region 批量操作

    /// <summary>
    /// 批量启用任务
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/enable")]
    [Operation("批量启用", OperationActionType.Ajax, confirmText: "确定要批量启用选中的任务吗？", isBulkOperation: true, Icon = "fa-solid fa-play")]
    [DisplayName("批量启用任务")]
    public async Task<ActionResult<ApiResponse>> BatchEnableTasks([FromBody] BatchTaskRequest request)
    {
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(ApiResponse.Error(400, "请选择要操作的任务"));
        }

        var results = new BatchOperationResult();
        foreach (var taskId in request.TaskIds)
        {
            try
            {
                var success = await _taskService.EnableTaskAsync(taskId);
                if (success)
                {
                    results.SuccessCount++;
                    results.SuccessIds.Add(taskId);
                }
                else
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "任务不存在" });
                }
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = ex.Message });
            }
        }

        return Ok(ApiResponse<object>.Success(results, $"批量启用完成：成功 {results.SuccessCount} 个，失败 {results.FailedCount} 个"));
    }

    /// <summary>
    /// 批量禁用任务
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/disable")]
    [Operation("批量禁用", OperationActionType.Ajax, confirmText: "确定要批量禁用选中的任务吗？", isBulkOperation: true, Icon = "fa-solid fa-pause")]
    [DisplayName("批量禁用任务")]
    public async Task<ActionResult<ApiResponse>> BatchDisableTasks([FromBody] BatchTaskRequest request)
    {
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(ApiResponse.Error(400, "请选择要操作的任务"));
        }

        var results = new BatchOperationResult();
        foreach (var taskId in request.TaskIds)
        {
            try
            {
                var success = await _taskService.DisableTaskAsync(taskId);
                if (success)
                {
                    results.SuccessCount++;
                    results.SuccessIds.Add(taskId);
                }
                else
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "任务不存在" });
                }
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = ex.Message });
            }
        }

        return Ok(ApiResponse<object>.Success(results, $"批量禁用完成：成功 {results.SuccessCount} 个，失败 {results.FailedCount} 个"));
    }

    /// <summary>
    /// 批量触发任务执行
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/trigger")]
    [Operation("批量执行", OperationActionType.Ajax, confirmText: "确定要批量执行选中的任务吗？", isBulkOperation: true, Icon = "fa-solid fa-rocket")]
    [DisplayName("批量触发任务")]
    public async Task<ActionResult<ApiResponse>> BatchTriggerTasks([FromBody] BatchTaskRequest request)
    {
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(ApiResponse.Error(400, "请选择要操作的任务"));
        }

        var results = new BatchTriggerResult();
        var authHeader = Request.Headers["Authorization"].ToString();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(ApiResponse.Error(401, "未提供有效的认证令牌"));
        }
        
        var token = authHeader.Substring("Bearer ".Length).Trim();

        foreach (var taskId in request.TaskIds)
        {
            try
            {
                // 查询任务所属服务
                var serviceName = await _registry.GetTaskServiceNameAsync(taskId);
                
                if (string.IsNullOrEmpty(serviceName))
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "无法确定任务所属服务" });
                    continue;
                }
                
                // 构建执行端点 URL
                var serviceUrl = $"http://{serviceName}";
                var executeUrl = $"{serviceUrl}/api/scheduled-tasks/execute/{taskId}";
                
                // 使用 HttpClient 调用
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
                
                var response = await httpClient.PostAsync(executeUrl, null);
                
                if (response.IsSuccessStatusCode)
                {
                    results.SuccessCount++;
                    results.SuccessIds.Add(taskId);
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = $"触发失败: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = ex.Message });
            }
        }

        return Ok(ApiResponse<object>.Success(results, $"批量触发完成：成功 {results.SuccessCount} 个，失败 {results.FailedCount} 个"));
    }

    /// <summary>
    /// 批量删除任务
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", OperationActionType.Ajax, confirmText: "确定要批量删除选中的任务吗？此操作不可恢复！", isBulkOperation: true, Icon = "fa-solid fa-trash")]
    [DisplayName("批量删除任务")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteTasks([FromBody] BatchTaskRequest request)
    {
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return BadRequest(ApiResponse.Error(400, "请选择要操作的任务"));
        }

        var results = new BatchOperationResult();
        foreach (var taskId in request.TaskIds)
        {
            try
            {
                var task = await _taskService.GetTaskAsync(taskId);
                if (task == null)
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "任务不存在" });
                    continue;
                }

                if (task.IsFromConfiguration)
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "配置文件中的任务不能删除" });
                    continue;
                }

                var success = await _taskService.DeleteTaskAsync(taskId);
                if (success)
                {
                    results.SuccessCount++;
                    results.SuccessIds.Add(taskId);
                }
                else
                {
                    results.FailedCount++;
                    results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = "删除失败" });
                }
            }
            catch (Exception ex)
            {
                results.FailedCount++;
                results.FailedItems.Add(new BatchOperationFailedItem { TaskId = taskId, Reason = ex.Message });
            }
        }

        return Ok(ApiResponse<object>.Success(results, $"批量删除完成：成功 {results.SuccessCount} 个，失败 {results.FailedCount} 个"));
    }

    #endregion
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

/// <summary>
/// 批量任务操作请求
/// </summary>
public class BatchTaskRequest
{
    /// <summary>
    /// 任务ID列表
    /// </summary>
    [DisplayName("任务ID列表")]
    [Required(ErrorMessage = "任务ID列表不能为空")]
    public List<string> TaskIds { get; set; } = new();
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BatchOperationResult
{
    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 成功的任务ID列表
    /// </summary>
    public List<string> SuccessIds { get; set; } = new();

    /// <summary>
    /// 失败的任务列表
    /// </summary>
    public List<BatchOperationFailedItem> FailedItems { get; set; } = new();
}

/// <summary>
/// 批量触发结果
/// </summary>
public class BatchTriggerResult : BatchOperationResult
{
    /// <summary>
    /// 触发的执行ID列表
    /// </summary>
    public List<string> ExecutionIds { get; set; } = new();
}

/// <summary>
/// 批量操作失败项
/// </summary>
public class BatchOperationFailedItem
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 任务分组信息
/// </summary>
public class TaskGroupInfo
{
    /// <summary>
    /// 分组名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务数量
    /// </summary>
    public int TaskCount { get; set; }

    /// <summary>
    /// 启用任务数量
    /// </summary>
    public int EnabledCount { get; set; }

    /// <summary>
    /// 禁用任务数量
    /// </summary>
    public int DisabledCount { get; set; }
}

/// <summary>
/// 批量设置分组请求
/// </summary>
public class BatchSetGroupRequest
{
    /// <summary>
    /// 任务ID列表
    /// </summary>
    [DisplayName("任务ID列表")]
    [Required(ErrorMessage = "任务ID列表不能为空")]
    public List<string> TaskIds { get; set; } = new();

    /// <summary>
    /// 分组名称（为空则表示移除分组）
    /// </summary>
    [DisplayName("分组名称")]
    public string? GroupName { get; set; }
}

/// <summary>
/// Cron构建请求
/// </summary>
public class CronBuilderRequest
{
    /// <summary>
    /// 秒（0-59, *, */n）
    /// </summary>
    [DisplayName("秒")]
    public string Second { get; set; } = "0";

    /// <summary>
    /// 分钟（0-59, *, */n）
    /// </summary>
    [DisplayName("分钟")]
    public string Minute { get; set; } = "*";

    /// <summary>
    /// 小时（0-23, *, */n）
    /// </summary>
    [DisplayName("小时")]
    public string Hour { get; set; } = "*";

    /// <summary>
    /// 日（1-31, *, ?）
    /// </summary>
    [DisplayName("日")]
    public string DayOfMonth { get; set; } = "*";

    /// <summary>
    /// 月（1-12, *）
    /// </summary>
    [DisplayName("月")]
    public string Month { get; set; } = "*";

    /// <summary>
    /// 周（0-6, *, ?，其中0=周日）
    /// </summary>
    [DisplayName("周")]
    public string DayOfWeek { get; set; } = "*";
}

/// <summary>
/// Cron编辑器选项
/// </summary>
public class CronEditorOptions
{
    /// <summary>
    /// 秒选项
    /// </summary>
    public List<CronOption> Seconds { get; set; } = new();

    /// <summary>
    /// 分钟选项
    /// </summary>
    public List<CronOption> Minutes { get; set; } = new();

    /// <summary>
    /// 小时选项
    /// </summary>
    public List<CronOption> Hours { get; set; } = new();

    /// <summary>
    /// 日选项
    /// </summary>
    public List<CronOption> DaysOfMonth { get; set; } = new();

    /// <summary>
    /// 月选项
    /// </summary>
    public List<CronOption> Months { get; set; } = new();

    /// <summary>
    /// 周选项
    /// </summary>
    public List<CronOption> DaysOfWeek { get; set; } = new();

    /// <summary>
    /// 常用模式
    /// </summary>
    public List<CronPattern> CommonPatterns { get; set; } = new();
}

/// <summary>
/// Cron选项
/// </summary>
public class CronOption
{
    /// <summary>
    /// 值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 显示标签
    /// </summary>
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Cron模式
/// </summary>
public class CronPattern
{
    /// <summary>
    /// 模式名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cron表达式
    /// </summary>
    public string Expression { get; set; } = string.Empty;
}
