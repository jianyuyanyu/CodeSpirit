using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Services;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 工作流定义管理控制器
/// </summary>
[DisplayName("工作流定义管理")]
[Navigation(Icon = "fa-solid fa-sitemap")]
public class WorkflowDefinitionsController : ApiControllerBase
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workflowDefinitionService">工作流定义服务</param>
    public WorkflowDefinitionsController(IWorkflowDefinitionService workflowDefinitionService)
    {
        _workflowDefinitionService = workflowDefinitionService;
    }

    /// <summary>
    /// 获取工作流定义列表
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>工作流定义分页列表</returns>
    [HttpGet]
    [DisplayName("获取工作流定义列表")]
    public async Task<ActionResult<ApiResponse<PageList<WorkflowDefinitionDto>>>> GetWorkflowDefinitions([FromQuery] WorkflowDefinitionQueryDto query)
    {
        var result = await _workflowDefinitionService.GetPagedListAsync(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取工作流定义详情
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>工作流定义详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取工作流定义详情")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> GetWorkflowDefinition(long id)
    {
        var result = await _workflowDefinitionService.GetAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建工作流定义
    /// </summary>
    /// <param name="dto">创建工作流定义DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建工作流定义")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> CreateWorkflowDefinition(CreateWorkflowDefinitionDto dto)
    {
        var result = await _workflowDefinitionService.CreateAsync(dto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新工作流定义
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <param name="dto">更新工作流定义DTO</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新工作流定义")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> UpdateWorkflowDefinition(long id, UpdateWorkflowDefinitionDto dto)
    {
        await _workflowDefinitionService.UpdateAsync(id, dto);
        var result = await _workflowDefinitionService.GetAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 删除工作流定义
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除工作流定义")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkflowDefinition(long id)
    {
        await _workflowDefinitionService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 启用工作流定义
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/enable")]
    [Operation("启用", "ajax", null, "确定要启用此工作流吗？", "!isEnabled")]
    [DisplayName("启用工作流定义")]
    public async Task<ActionResult<ApiResponse>> EnableWorkflowDefinition(long id)
    {
        await _workflowDefinitionService.SetEnabledAsync(id, true);
        return SuccessResponse("工作流定义已启用");
    }

    /// <summary>
    /// 禁用工作流定义
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/disable")]
    [Operation("禁用", "ajax", null, "确定要禁用此工作流吗？", "isEnabled")]
    [DisplayName("禁用工作流定义")]
    public async Task<ActionResult<ApiResponse>> DisableWorkflowDefinition(long id)
    {
        await _workflowDefinitionService.SetEnabledAsync(id, false);
        return SuccessResponse("工作流定义已禁用");
    }

    /// <summary>
    /// 复制工作流定义
    /// </summary>
    /// <param name="id">源工作流定义ID</param>
    /// <param name="dto">复制参数</param>
    /// <returns>复制结果</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "form")]
    [DisplayName("复制工作流定义")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> CopyWorkflowDefinition(long id, CopyWorkflowDefinitionDto dto)
    {
        var result = await _workflowDefinitionService.CopyAsync(id, dto.Name, dto.Code);
        var resultDto = await _workflowDefinitionService.GetAsync(result.Id);
        return SuccessResponse(resultDto);
    }
}

/// <summary>
/// 复制工作流定义DTO
/// </summary>
public class CopyWorkflowDefinitionDto
{
    /// <summary>
    /// 新工作流名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("新工作流名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 新工作流代码
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("新工作流代码")]
    public string Code { get; set; } = string.Empty;
}
