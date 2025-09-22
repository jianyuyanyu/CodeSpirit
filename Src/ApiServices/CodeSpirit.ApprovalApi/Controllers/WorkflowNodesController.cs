using CodeSpirit.ApprovalApi.Dtos.WorkflowNode;
using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.ApprovalApi.Controllers;

/// <summary>
/// 审批节点管理控制器
/// </summary>
[DisplayName("审批节点管理")]
[Navigation(Icon = "fa-solid fa-sitemap")]
public class WorkflowNodesController : ApiControllerBase
{
    private readonly IWorkflowNodeService _workflowNodeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workflowNodeService">工作流节点服务</param>
    public WorkflowNodesController(IWorkflowNodeService workflowNodeService)
    {
        _workflowNodeService = workflowNodeService;
    }

    /// <summary>
    /// 获取工作流节点列表
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>工作流节点分页列表</returns>
    [HttpGet]
    [DisplayName("获取工作流节点列表")]
    public async Task<ActionResult<ApiResponse<PageList<WorkflowNodeDto>>>> GetWorkflowNodes([FromQuery] WorkflowNodeQueryDto query)
    {
        var result = await _workflowNodeService.GetPagedListAsync(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取工作流节点详情
    /// </summary>
    /// <param name="id">工作流节点ID</param>
    /// <returns>工作流节点详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取工作流节点详情")]
    public async Task<ActionResult<ApiResponse<WorkflowNodeDto>>> GetWorkflowNode(long id)
    {
        var result = await _workflowNodeService.GetAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 创建工作流节点
    /// </summary>
    /// <param name="dto">创建工作流节点DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建工作流节点")]
    public async Task<ActionResult<ApiResponse<WorkflowNodeDto>>> CreateWorkflowNode(CreateWorkflowNodeDto dto)
    {
        var result = await _workflowNodeService.CreateAsync(dto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 更新工作流节点
    /// </summary>
    /// <param name="id">工作流节点ID</param>
    /// <param name="dto">更新工作流节点DTO</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新工作流节点")]
    public async Task<ActionResult<ApiResponse<WorkflowNodeDto>>> UpdateWorkflowNode(long id, UpdateWorkflowNodeDto dto)
    {
        await _workflowNodeService.UpdateAsync(id, dto);
        var result = await _workflowNodeService.GetAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 删除工作流节点
    /// </summary>
    /// <param name="id">工作流节点ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除工作流节点")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkflowNode(long id)
    {
        await _workflowNodeService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 复制工作流节点
    /// </summary>
    /// <param name="id">源工作流节点ID</param>
    /// <param name="dto">复制参数</param>
    /// <returns>复制结果</returns>
    [HttpPost("{id}/copy")]
    [Operation("复制", "form")]
    [DisplayName("复制工作流节点")]
    public async Task<ActionResult<ApiResponse<WorkflowNodeDto>>> CopyWorkflowNode(long id, CopyWorkflowNodeWithWorkflowIdDto dto)
    {
        var sourceNode = await _workflowNodeService.GetAsync(id);
        var createDto = new CreateWorkflowNodeDto
        {
            WorkflowDefinitionId = dto.WorkflowDefinitionId,
            Name = dto.Name,
            NodeType = sourceNode.NodeType,
            ApprovalMode = sourceNode.ApprovalMode,
            Configuration = sourceNode.Configuration
        };
        var result = await _workflowNodeService.CreateAsync(createDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取指定工作流的节点列表
    /// </summary>
    /// <param name="workflowId">工作流定义ID</param>
    /// <returns>节点列表</returns>
    [HttpGet("by-workflow/{workflowId}")]
    [DisplayName("获取指定工作流的节点列表")]
    public async Task<ActionResult<ApiResponse<List<WorkflowNodeDto>>>> GetNodesByWorkflow(long workflowId)
    {
        var result = await _workflowNodeService.GetByWorkflowDefinitionIdAsync(workflowId);
        return SuccessResponse(result);
    }
}

/// <summary>
/// 复制工作流节点DTO
/// </summary>
public class CopyWorkflowNodeDto
{
    /// <summary>
    /// 新节点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("新节点名称")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 复制工作流节点（带工作流ID）DTO
/// </summary>
public class CopyWorkflowNodeWithWorkflowIdDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 新节点名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("新节点名称")]
    public string Name { get; set; } = string.Empty;
}

