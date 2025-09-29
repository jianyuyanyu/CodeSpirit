using CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;
using CodeSpirit.ApprovalApi.Dtos.WorkflowNode;
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
    private readonly IWorkflowNodeService _workflowNodeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workflowDefinitionService">工作流定义服务</param>
    /// <param name="workflowNodeService">工作流节点服务</param>
    public WorkflowDefinitionsController(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowNodeService workflowNodeService)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowNodeService = workflowNodeService;
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
    [HeaderOperation("AI创建", "form", Icon = "fa-solid fa-magic")]
    public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> CreateWorkflowDefinition(CreateWorkflowDefinitionDto dto)
    {
        var result = await _workflowDefinitionService.CreateAsync(dto);
        return SuccessResponse(result);
    }

    ///// <summary>
    ///// 更新工作流定义
    ///// </summary>
    ///// <param name="id">工作流定义ID</param>
    ///// <param name="dto">更新工作流定义DTO</param>
    ///// <returns>更新结果</returns>
    //[HttpPut("{id}")]
    //[DisplayName("更新工作流定义")]
    //public async Task<ActionResult<ApiResponse<WorkflowDefinitionDto>>> UpdateWorkflowDefinition(long id, UpdateWorkflowDefinitionDto dto)
    //{
    //    await _workflowDefinitionService.UpdateAsync(id, dto);
    //    var result = await _workflowDefinitionService.GetAsync(id);
    //    return SuccessResponse(result);
    //}

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
    /// 快速保存工作流定义
    /// </summary>
    /// <param name="request">快速保存请求</param>
    /// <returns>保存结果</returns>
    [HttpPatch("quickSave")]
    [DisplayName("快速保存工作流定义")]
    public async Task<ActionResult<ApiResponse>> QuickSaveWorkflowDefinitions([FromBody] WorkflowDefinitionQuickSaveRequestDto request)
    {
        await _workflowDefinitionService.QuickSaveWorkflowDefinitionsAsync(request);
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

    /// <summary>
    /// 表单设计
    /// </summary>
    /// <param name="dto">表单设计参数</param>
    /// <returns>设计结果</returns>
    [HttpPost("{id}/form-design")]
    [Operation("表单设计", actionType: OperationActionType.Form, Icon = "fa-solid fa-palette",DialogSize = DialogSize.Full)]
    [DisplayName("表单设计")]
    public async Task<ActionResult<ApiResponse<FormDesignDto>>> DesignForm(long id, FormDesignDto dto)
    {
        // 获取工作流定义
        var workflow = await _workflowDefinitionService.GetAsync(id);
        if (workflow == null)
        {
            throw new BusinessException("工作流定义不存在");
        }

        // 更新工作流的表单Schema
        await _workflowDefinitionService.UpdateFormSchemaAsync(id, dto.FormSchema, dto.Name);

        return SuccessResponse(dto);
    }

    /// <summary>
    /// 节点管理操作
    /// </summary>
    /// <returns>操作结果</returns>
    [Operation("节点管理", "link", "/approval/workflowNodes?WorkflowDefinitionId=$id", null, Icon = "fa-solid fa-share-alt")]
    [DisplayName("节点管理")]
    public ActionResult<ApiResponse> Nodes_Manager()
    {
        return SuccessResponse();
    }

    #region 流程设计和预览

    /// <summary>
    /// 保存流程设计
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <param name="dto">流程设计DTO</param>
    /// <returns>保存结果</returns>
    [HttpPost("{id}/process-design")]
    [DisplayName("保存流程设计")]
    public async Task<ActionResult<ApiResponse>> SaveProcessDesign(long id, WorkflowProcessDesignDto dto)
    {
        // 确保DTO中的工作流ID与路径参数一致
        dto.WorkflowDefinitionId = id;

        await _workflowNodeService.SaveProcessDesignAsync(dto);
        return SuccessResponse("流程设计保存成功");
    }

    /// <summary>
    /// 获取工作流预览数据（包含工作流信息和节点数据）
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>预览数据</returns>
    [HttpGet("{id}/preview-data")]
    [DisplayName("获取工作流预览数据")]
    public async Task<ActionResult<ApiResponse<FrontendPreviewDataDto>>> GetWorkflowPreviewData(long id)
    {
        var previewData = await _workflowNodeService.GetWorkflowPreviewAsync(id);

        // 转换数据格式以适配前端页面
        var adaptedData = ConvertToFrontendFormat(previewData);

        return SuccessResponse(adaptedData);
    }

    /// <summary>
    /// 获取流程预览UI配置
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>流程预览UI配置</returns>
    [HttpGet("{id}/preview-ui")]
    [Operation("流程预览", actionType: OperationActionType.Link, "/$tenantId/approval/workflow-preview/$id", Blank = true)]
    [DisplayName("流程预览")]
    public ActionResult<ApiResponse> GetWorkflowPreviewUI(long id)
    {
        return SuccessResponse();
    }

    /// <summary>
    /// 获取流程设计器UI配置
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>流程设计器UI配置</returns>
    [HttpGet("{id}/designer-ui")]
    [Operation("节点管理", "service", "/api/WorkflowDefinitions/{id}/designer-ui")]
    [DisplayName("节点管理")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowDesignerUI(long id)
    {
        var workflowDefinition = await _workflowDefinitionService.GetAsync(id);
        var existingNodes = await _workflowNodeService.GetWorkflowPreviewAsync(id);

        var uiConfig = new
        {
            type = "page",
            title = $"流程设计器 - {workflowDefinition.Name}",
            body = new[]
            {
                new
                {
                    type = "panel",
                    title = "流程设计",
                    className = "m-b-md",
                    body = new
                    {
                        title = "",
                        type = "form",
                        api = $"post:/approval/api/approval/WorkflowDefinitions/{id}/process-design",
                        initApi = $"get:/approval/api/approval/WorkflowDefinitions/{id}/preview-data",
                        body = new object[]
                        {
                            new
                            {
                                type = "hidden",
                                name = "workflowDefinitionId",
                                value = id
                            },
                            new
                            {
                                type = "wrapper",
                                body = new[]
                                {
                                    GenerateNodeConfigForm(id)
                                }
                            },
                            new
                            {
                                type = "divider"
                            },
                            new
                            {
                                type = "button-group",
                                buttons = new object[]
                                {
                                    new
                                    {
                                        type = "submit",
                                        label = "保存设计",
                                        level = "primary"
                                    },
                                    new
                                    {
                                        type = "button",
                                        label = "预览流程",
                                        level = "info",
                                        actionType = "link",
                                        link = $"/approval/workflow-preview/{id}",
                                        blank = true
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        return SuccessResponse((object)uiConfig);
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 转换为前端格式
    /// </summary>
    /// <param name="previewData">预览数据</param>
    /// <returns>前端格式数据</returns>
    private FrontendPreviewDataDto ConvertToFrontendFormat(WorkflowPreviewDto previewData)
    {
        return new FrontendPreviewDataDto
        {
            Workflow = previewData.Workflow,
            Nodes = previewData.Nodes.Select(node => new FrontendNodeDto
            {
                Id = node.Id,
                Name = node.Name,
                NodeType = node.Type,
                ApprovalMode = node.ApprovalMode,
                Configuration = node.Configuration,
                Approvers = node.Approvers.Select(approver => new FrontendApproverDto
                {
                    Type = approver.Type,
                    Value = approver.Value,
                    Name = approver.Name
                }).ToList(),
                Conditions = node.Conditions.Select(condition => new FrontendConditionDto
                {
                    Expression = condition.Expression,
                    NextNodeName = condition.NextNodeName,
                    Description = condition.Description
                }).ToList()
            }).ToList()
        };
    }


    /// <summary>
    /// 生成节点配置表单
    /// </summary>
    /// <param name="workflowId">工作流ID</param>
    /// <returns>节点配置表单</returns>
    private object GenerateNodeConfigForm(long workflowId)
    {
        return new
        {
            type = "container",
            body = new[]
            {
                new
                {
                    type = "input-table",
                    name = "nodes",
                    label = "流程节点",
                    description = "配置工作流的各个节点",
                    columns = new object[]
                    {
                        new
                        {
                            name = "name",
                            label = "节点名称",
                            type = "input-text",
                            required = true,
                            placeholder = "请输入节点名称"
                        },
                        new
                        {
                            name = "nodeType",
                            label = "节点类型",
                            type = "select",
                            required = true,
                            options = new[]
                            {
                                new { label = "开始节点", value = "Start" },
                                new { label = "审批节点", value = "Approval" },
                                new { label = "条件节点", value = "Condition" },
                                new { label = "并行网关", value = "ParallelGateway" },
                                new { label = "排他网关", value = "ExclusiveGateway" },
                                new { label = "抄送节点", value = "CarbonCopy" },
                                new { label = "结束节点", value = "End" }
                            }
                        },
                        new
                        {
                            name = "approvalMode",
                            label = "审批模式",
                            type = "select",
                            visibleOn = "${nodeType == 'Approval'}",
                            options = new[]
                            {
                                new { label = "串行审批", value = "Sequential" },
                                new { label = "并行审批", value = "Parallel" },
                                new { label = "会签", value = "CounterSign" },
                                new { label = "或签", value = "OrSign" }
                            },
                            value = "Sequential"
                        },
                        new
                        {
                            name = "approvers",
                            label = "审批人配置",
                            type = "input-table",
                            visibleOn = "${nodeType == 'Approval' || nodeType == 'CarbonCopy'}",
                            columns = new object[]
                            {
                                new
                                {
                                    name = "type",
                                    label = "审批人类型",
                                    type = "select",
                                    options = new[]
                                    {
                                        new { label = "指定用户", value = "User" },
                                        new { label = "角色", value = "Role" },
                                        new { label = "部门", value = "Department" },
                                        new { label = "发起人", value = "Initiator" },
                                        new { label = "发起人上级", value = "InitiatorSuperior" },
                                        new { label = "动态表达式", value = "Expression" }
                                    },
                                    value = "User"
                                },
                                new
                                {
                                    name = "value",
                                    label = "审批人值",
                                    type = "input-text",
                                    placeholder = "根据审批人类型输入对应的值"
                                },
                                new
                                {
                                    name = "name",
                                    label = "审批人名称",
                                    type = "input-text",
                                    placeholder = "审批人显示名称"
                                }
                            },
                            addable = true,
                            removable = true,
                            addButtonText = "添加审批人"
                        },
                        new
                        {
                            name = "conditions",
                            label = "条件配置",
                            type = "input-table",
                            visibleOn = "${nodeType == 'Condition' || nodeType == 'ExclusiveGateway'}",
                            columns = new object[]
                            {
                                new
                                {
                                    name = "expression",
                                    label = "条件表达式",
                                    type = "input-text",
                                    placeholder = "例如: amount > 1000"
                                },
                                new
                                {
                                    name = "nextNodeName",
                                    label = "下一节点名称",
                                    type = "input-text",
                                    placeholder = "满足条件时跳转的节点名称"
                                },
                                new
                                {
                                    name = "description",
                                    label = "条件描述",
                                    type = "input-text",
                                    placeholder = "条件的显示描述"
                                }
                            },
                            addable = true,
                            removable = true,
                            addButtonText = "添加条件"
                        },
                        new
                        {
                            name = "configuration",
                            label = "节点配置",
                            type = "textarea",
                            placeholder = "节点的额外配置（JSON格式）",
                            minRows = 3,
                            value = "{}"
                        }
                    },
                    addable = true,
                    removable = true,
                    draggable = true,
                    addButtonText = "添加节点"
                }
            }
        };
    }

    #endregion
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

