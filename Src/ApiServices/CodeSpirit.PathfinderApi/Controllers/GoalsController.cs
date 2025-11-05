using CodeSpirit.Core.Enums;
using CodeSpirit.PathfinderApi.Dtos;
using CodeSpirit.PathfinderApi.Dtos.Goal;
using CodeSpirit.PathfinderApi.Dtos.Task;
using CodeSpirit.PathfinderApi.Services.Interfaces;
using CodeSpirit.Shared.Repositories;

namespace CodeSpirit.PathfinderApi.Controllers;

/// <summary>
/// 目标管理控制器
/// </summary>
[DisplayName("目标管理")]
[Navigation(Icon = "fa-solid fa-bullseye", PlatformType = PlatformType.Tenant)]
public class GoalsController : ApiControllerBase
{
    private readonly IGoalService _goalService;
    private readonly ITaskService _taskService;
    private readonly ILogger<GoalsController> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="goalService">目标服务</param>
    /// <param name="taskService">任务服务</param>
    /// <param name="logger">日志记录器</param>
    public GoalsController(
        IGoalService goalService,
        ITaskService taskService,
        ILogger<GoalsController> logger)
    {
        _goalService = goalService;
        _taskService = taskService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取目标列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>目标列表</returns>
    [HttpGet]
    [DisplayName("获取目标列表")]
    public async Task<ActionResult<ApiResponse<PageList<GoalDto>>>> GetGoals([FromQuery] GoalQueryDto queryDto)
    {
        var goals = await _goalService.GetGoalsAsync(queryDto);
        return SuccessResponse(goals);
    }
    
    /// <summary>
    /// 获取目标详情
    /// </summary>
    /// <param name="id">目标ID</param>
    /// <returns>目标详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取目标详情")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> Detail(Guid id)
    {
        var goal = await _goalService.GetAsync(id);
        if (goal == null)
        {
            return BadResponse<GoalDto>("目标不存在");
        }
        return SuccessResponse(goal);
    }
    
    /// <summary>
    /// 创建目标（前端AI表单填充已完成评估）
    /// </summary>
    /// <param name="dto">创建目标DTO</param>
    /// <returns>创建的目标</returns>
    [HttpPost]
    [DisplayName("创建目标")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> CreateGoal(CreateGoalDto dto)
    {
        try
        {
            _logger.LogInformation("开始创建目标: {Description}", dto.Description);
            
            // 验证评分：三个维度都必须>=7
            if (!dto.ClarityScore.HasValue || !dto.ExecutabilityScore.HasValue || !dto.CompletenessScore.HasValue)
            {
                return BadResponse<GoalDto>("目标评估未完成，请先使用AI优化功能完成可行性评估");
            }
            
            if (dto.ClarityScore.Value < 7 || dto.ExecutabilityScore.Value < 7 || dto.CompletenessScore.Value < 7)
            {
                return BadResponse<GoalDto>($"目标可行性评估未通过（明确性:{dto.ClarityScore}/10, 可执行性:{dto.ExecutabilityScore}/10, 完整性:{dto.CompletenessScore}/10）。建议优化目标描述后重试");
            }
            
            // 创建目标（包含前端AI评估结果）
            var goal = await _goalService.CreateGoalAsync(dto);
            _logger.LogInformation("目标创建成功，ID: {GoalId}, 综合评分: {FeasibilityScore}", goal.Id, goal.FeasibilityScore);
            
            // 根据评估结果构建反馈消息
            string message = BuildFeedbackMessage(goal);
            
            return SuccessResponse(goal, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建目标失败");
            return BadResponse<GoalDto>("创建目标失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 构建反馈消息
    /// </summary>
    /// <param name="goal">目标信息</param>
    /// <returns>反馈消息</returns>
    private string BuildFeedbackMessage(GoalDto goal)
    {
        // 如果有评分且都>=7，表示评估通过
        if (goal.ClarityScore >= 7 && goal.ExecutabilityScore >= 7 && goal.CompletenessScore >= 7)
        {
            return "🎉 目标创建成功！可行性评估通过，您可以开始进行AI任务拆解了。";
        }
        
        // 如果有评分但未完全通过
        if (goal.ClarityScore.HasValue || goal.ExecutabilityScore.HasValue || goal.CompletenessScore.HasValue)
        {
            return "⚠️ 目标已创建，但部分评估指标较低。建议优化目标后再进行AI任务拆解。";
        }
        
        // 没有评分
        return "✅ 目标已创建。";
    }
    
    /// <summary>
    /// 更新目标
    /// </summary>
    /// <param name="id">目标ID</param>
    /// <param name="dto">更新DTO</param>
    /// <returns>更新后的目标</returns>
    [HttpPut("{id}")]
    [DisplayName("更新目标")]
    public async Task<ActionResult<ApiResponse<GoalDto>>> UpdateGoal(Guid id, UpdateGoalDto dto)
    {
        try
        {
            var goal = await _goalService.UpdateGoalAsync(id, dto);
            return SuccessResponse(goal);
        }
        catch (BusinessException ex)
        {
            return BadResponse<GoalDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新目标失败: {GoalId}", id);
            return BadResponse<GoalDto>("更新目标失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 删除目标
    /// </summary>
    /// <param name="id">目标ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除目标")]
    public async Task<ActionResult<ApiResponse>> DeleteGoal(Guid id)
    {
        try
        {
            await _goalService.DeleteAsync(id);
            return SuccessResponse("删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除目标失败: {GoalId}", id);
            return BadResponse("删除目标失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 查看任务管理
    /// </summary>
    [HttpGet("tasks")]
    [DisplayName("任务管理")]
    [Operation("任务管理", OperationActionType.Link, "/pathfinder/tasks?goalId=${id}", Icon = "fa-solid fa-list-check")]
    public void ViewTasks()
    {
        // 此方法仅用于在前端生成跳转按钮，不会被实际调用
    }
    
    /// <summary>
    /// AI任务拆解（支持AI表单填充）
    /// </summary>
    /// <param name="formDto">任务拆解表单DTO</param>
    /// <returns>创建的任务列表</returns>
    [HttpPost("breakdown")]
    [DisplayName("AI任务拆解")]
    [Operation("AI任务拆解", OperationActionType.Form, 
        Data = "{\"goalId\":\"${id}\",\"goalTitle\":\"${title}\",\"goalDescription\":\"${description}\",\"goalCategory\":\"${category}\",\"goalTargetDate\":\"${targetDate}\"}",
        DialogSize = DialogSize.XL,
        Icon = "fa-solid fa-wand-magic-sparkles")]
    public async Task<ActionResult<ApiResponse<List<TaskDto>>>> BreakdownTasks(Dtos.Task.TaskBreakdownFormDto formDto)
    {
        try
        {
            _logger.LogInformation("开始AI任务拆解并保存: GoalId={GoalId}, Granularity={Granularity}, TaskCount={TaskCount}", 
                formDto.GoalId, formDto.Granularity, formDto.Tasks.Count);
            
            // 验证目标是否存在
            var goal = await _goalService.GetAsync(formDto.GoalId);
            if (goal == null)
            {
                return BadResponse<List<TaskDto>>("目标不存在");
            }
            
            // 转换TaskItemDto为CreateTaskDto
            var createTaskDtos = formDto.Tasks.Select(t => new Dtos.Task.CreateTaskDto
            {
                GoalId = formDto.GoalId,
                Title = t.Title,
                Description = t.Description,
                Priority = t.Priority,
                EstimatedStartTime = t.EstimatedStartTime,
                EstimatedEndTime = t.EstimatedEndTime,
                DependsOn = t.DependsOn
            }).ToList();
            
            // 批量创建任务
            var batchRequest = new Dtos.Task.BatchCreateTasksRequest
            {
                GoalId = formDto.GoalId,
                Tasks = createTaskDtos
            };
            
            var createdTasks = await _taskService.BatchCreateTasksAsync(batchRequest);
            
            _logger.LogInformation("AI任务拆解完成，成功创建 {Count} 个任务", createdTasks.Count);
            
            return SuccessResponse(createdTasks, $"✅ 成功创建 {createdTasks.Count} 个任务！");
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "AI任务拆解失败（业务异常）: GoalId={GoalId}", formDto.GoalId);
            return BadResponse<List<TaskDto>>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI任务拆解失败: GoalId={GoalId}", formDto.GoalId);
            return BadResponse<List<TaskDto>>("任务拆解失败：" + ex.Message);
        }
    }
    
    /// <summary>
    /// 导出目标列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>导出数据</returns>
    [HttpGet("Export")]
    [DisplayName("导出目标列表")]
    public async Task<ActionResult<ApiResponse<PageList<GoalDto>>>> Export([FromQuery] GoalQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;
        
        var goals = await _goalService.GetGoalsAsync(queryDto);
        
        if (goals.Items.Count == 0)
        {
            return BadResponse<PageList<GoalDto>>("没有数据可供导出");
        }
        
        return SuccessResponse(goals);
    }
}
