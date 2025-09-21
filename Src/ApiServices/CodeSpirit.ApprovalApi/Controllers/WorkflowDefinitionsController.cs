using CodeSpirit.ApprovalApi.Dtos;
using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.Core.Attributes;

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

    #region 流程设计和预览

    ///// <summary>
    ///// 保存流程设计
    ///// </summary>
    ///// <param name="dto">流程设计DTO</param>
    ///// <returns>保存结果</returns>
    //[HttpPost("process-design")]
    //[Operation("保存流程设计", "form")]
    //[DisplayName("保存流程设计")]
    //public async Task<ActionResult<ApiResponse>> SaveProcessDesign(WorkflowProcessDesignDto dto)
    //{
    //    await _workflowNodeService.SaveProcessDesignAsync(dto);
    //    return SuccessResponse("流程设计保存成功");
    //}

    /// <summary>
    /// 获取工作流预览
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>预览数据</returns>
    [HttpGet("{id}/preview")]
    [DisplayName("获取工作流预览")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowPreview(long id)
    {
        var result = await _workflowNodeService.GetWorkflowPreviewAsync(id);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取工作流预览数据（用于前端页面渲染）
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>预览数据</returns>
    [HttpGet("{id}/preview-data")]
    [DisplayName("获取前端预览数据")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowPreviewData(long id)
    {
        try
        {
            var previewData = await _workflowNodeService.GetWorkflowPreviewAsync(id);

            // 转换数据格式以适配前端页面
            var adaptedData = new
            {
                nodes = ConvertNodesForFrontend(previewData)
            };

            return SuccessResponse((object)adaptedData);
        }
        catch (Exception)
        {
            return BadRequest(ApiResponse.Error(400, "获取工作流预览数据失败"));
        }
    }

    /// <summary>
    /// 获取流程预览UI配置
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>流程预览UI配置</returns>
    [HttpGet("{id}/preview-ui")]
    [Operation("流程预览", actionType: OperationActionType.Link, "/$tenantId/approval/workflow-preview/$id", Blank = true)]
    [DisplayName("流程预览")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowPreviewUI(long id)
    {
        // 验证工作流是否存在
        var workflowDefinition = await _workflowDefinitionService.GetAsync(id);
        if (workflowDefinition == null)
        {
            return NotFound(ApiResponse.Error(404, "工作流定义不存在"));
        }

        // 返回跳转配置
        var uiConfig = new
        {
            type = "action",
            actionType = OperationActionType.Link,
            url = $"/approval/workflow-preview/{id}",
            blank = true, // 在新窗口打开
            label = "预览工作流",
            level = "primary",
            icon = "fa fa-eye",
            tooltip = $"预览工作流: {workflowDefinition.Name}"
        };

        return SuccessResponse((object)uiConfig);
    }

    /// <summary>
    /// 获取流程设计器UI配置
    /// </summary>
    /// <param name="id">工作流定义ID</param>
    /// <returns>流程设计器UI配置</returns>
    [HttpGet("{id}/designer-ui")]
    [Operation("流程设计器", "service", "/api/WorkflowDefinitions/{id}/designer-ui")]
    [DisplayName("流程设计器")]
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
                        type = "form",
                        api = $"/approval/api/approval/WorkflowDefinitions/process-design",
                        initApi = $"/approval/api/approval/WorkflowDefinitions/{id}/preview",
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
                                type = "tabs",
                                tabs = new[]
                                {
                                    new
                                    {
                                        title = "可视化设计",
                                        body = new[]
                                        {
                                            GenerateWorkflowDesignerConfig(id, existingNodes)
                                        }
                                    },
                                    new
                                    {
                                        title = "节点配置",
                                        body = new[]
                                        {
                                            GenerateNodeConfigForm(id)
                                        }
                                    }
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
                                        actionType = "dialog",
                                        dialog = new
                                        {
                                            title = "流程预览",
                                            size = "lg",
                                            body = new
                                            {
                                                type = "service",
                                                api = $"/approval/api/approval/WorkflowDefinitions/{id}/preview-ui"
                                            }
                                        }
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
    /// 转换节点数据格式以适配前端页面
    /// </summary>
    /// <param name="previewData">预览数据</param>
    /// <returns>转换后的节点数据</returns>
    private object ConvertNodesForFrontend(dynamic previewData)
    {
        try
        {
            if (previewData?.nodes == null)
                return new List<object>();

            var nodesList = ((IEnumerable<dynamic>)previewData.nodes).ToList();

            return nodesList.Select(node => new
            {
                id = node.id,
                name = node.name?.ToString() ?? "未命名节点",
                nodeType = node.type?.ToString() ?? "Approval",
                approvalMode = node.approvalMode?.ToString() ?? "Sequential",
                approvers = ConvertApproversForFrontend(node.approvers),
                conditions = ConvertConditionsForFrontend(node.conditions)
            }).ToList();
        }
        catch (Exception)
        {
            return new List<object>();
        }
    }

    /// <summary>
    /// 转换审批人数据格式
    /// </summary>
    /// <param name="approvers">审批人数据</param>
    /// <returns>转换后的审批人数据</returns>
    private object ConvertApproversForFrontend(dynamic approvers)
    {
        try
        {
            if (approvers == null)
                return new List<object>();

            var approversList = ((IEnumerable<dynamic>)approvers).ToList();

            return approversList.Select(approver => new
            {
                approverType = approver.type?.ToString() ?? "User",
                approverValue = approver.value?.ToString() ?? "",
                approverName = approver.name?.ToString() ?? approver.value?.ToString() ?? ""
            }).ToList();
        }
        catch (Exception)
        {
            return new List<object>();
        }
    }

    /// <summary>
    /// 转换条件数据格式
    /// </summary>
    /// <param name="conditions">条件数据</param>
    /// <returns>转换后的条件数据</returns>
    private object ConvertConditionsForFrontend(dynamic conditions)
    {
        try
        {
            if (conditions == null)
                return new List<object>();

            var conditionsList = ((IEnumerable<dynamic>)conditions).ToList();

            return conditionsList.Select(condition => new
            {
                expression = condition.expression?.ToString() ?? "",
                nextNodeName = condition.nextNodeName?.ToString() ?? "",
                description = condition.description?.ToString() ?? condition.expression?.ToString() ?? ""
            }).ToList();
        }
        catch (Exception)
        {
            return new List<object>();
        }
    }





    /// <summary>
    /// 生成工作流设计器配置
    /// </summary>
    /// <param name="workflowId">工作流ID</param>
    /// <param name="existingNodes">现有节点</param>
    /// <returns>设计器配置</returns>
    private object GenerateWorkflowDesignerConfig(long workflowId, dynamic existingNodes)
    {
        return new
        {
            type = "container",
            body = new object[]
            {
                new
                {
                    type = "alert",
                    level = "info",
                    body = "通过拖拽组件来设计您的工作流程。您可以添加审批节点、条件节点、网关等。"
                },
                new
                {
                    type = "divider"
                },
                new
                {
                    type = "grid",
                    columns = new object[]
                    {
                        new
                        {
                            md = 3,
                            body = new
                            {
                                type = "panel",
                                title = "组件库",
                                body = new object[]
                                {
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-play-circle antd-text-success' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>开始节点</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>工作流开始节点</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-user-check antd-text-primary' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>审批节点</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>需要人工审批的节点</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-question-circle antd-text-warning' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>条件节点</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>根据条件分支的节点</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-code-branch antd-text-info' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>并行网关</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>并行处理多个分支</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-random antd-text-secondary' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>排他网关</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>根据条件选择一个分支</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-copy antd-text-cyan' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>抄送节点</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>抄送给相关人员</div>" +
                                              "</div>"
                                    },
                                    new
                                    {
                                        type = "tpl",
                                        tpl = "<div class='workflow-component-item antd-card p-3 m-b-sm' draggable='true' style='border: 1px solid #d9d9d9; border-radius: 6px; cursor: move; background: #fff;'>" +
                                              "<i class='fa fa-stop-circle antd-text-danger' style='font-size: 16px; margin-right: 8px;'></i>" +
                                              "<strong class='antd-text-dark'>结束节点</strong>" +
                                              "<div class='antd-text-muted' style='font-size: 12px; margin-top: 4px;'>工作流结束节点</div>" +
                                              "</div>"
                                    }
                                }
                            }
                        },
                        new
                        {
                            md = 9,
                            body = new
                            {
                                type = "panel",
                                title = "设计画布",
                                body = new
                                {
                                    type = "container",
                                    className = "workflow-canvas",
                                    style = new
                                    {
                                        minHeight = "400px",
                                        border = "2px dashed #d9d9d9",
                                        borderRadius = "4px",
                                        position = "relative",
                                        backgroundColor = "#fafafa"
                                    },
                                    body = new[]
                                    {
                                        new
                                        {
                                            type = "tpl",
                                            tpl = "<div class='workflow-canvas-placeholder text-center p-lg'>" +
                                                  "<i class='fa fa-plus fa-2x text-muted m-b-sm'></i>" +
                                                  "<div class='text-muted'>拖拽左侧组件到此处开始设计流程</div>" +
                                                  "</div>"
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new
                {
                    type = "textarea",
                    name = "processConfig",
                    label = "流程配置JSON",
                    placeholder = "此处将自动生成流程配置JSON",
                    minRows = 10,
                    language = "json",
                    options = new
                    {
                        lineNumbers = true,
                        theme = "vs"
                    }
                }
            }
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
                    type = "input-array",
                    name = "nodes",
                    label = "流程节点",
                    description = "配置工作流的各个节点",
                    items = new
                    {
                        type = "container",
                        body = new object[]
                        {
                            new
                            {
                                type = "input-text",
                                name = "name",
                                label = "节点名称",
                                required = true,
                                placeholder = "请输入节点名称"
                            },
                            new
                            {
                                type = "select",
                                name = "nodeType",
                                label = "节点类型",
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
                                type = "select",
                                name = "approvalMode",
                                label = "审批模式",
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
                                type = "input-array",
                                name = "approvers",
                                label = "审批人配置",
                                visibleOn = "${nodeType == 'Approval' || nodeType == 'CarbonCopy'}",
                                items = new
                                {
                                    type = "container",
                                    body = new object[]
                                    {
                                        new
                                        {
                                            type = "select",
                                            name = "approverType",
                                            label = "审批人类型",
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
                                            type = "input-text",
                                            name = "approverValue",
                                            label = "审批人值",
                                            placeholder = "根据审批人类型输入对应的值"
                                        },
                                        new
                                        {
                                            type = "input-text",
                                            name = "approverName",
                                            label = "审批人名称",
                                            placeholder = "审批人显示名称"
                                        }
                                    }
                                }
                            },
                            new
                            {
                                type = "input-array",
                                name = "conditions",
                                label = "条件配置",
                                visibleOn = "${nodeType == 'Condition' || nodeType == 'ExclusiveGateway'}",
                                items = new
                                {
                                    type = "container",
                                    body = new[]
                                    {
                                        new
                                        {
                                            type = "input-text",
                                            name = "conditionExpression",
                                            label = "条件表达式",
                                            placeholder = "例如: amount > 1000"
                                        },
                                        new
                                        {
                                            type = "input-text",
                                            name = "conditionName",
                                            label = "条件名称",
                                            placeholder = "条件的显示名称"
                                        }
                                    }
                                }
                            },
                            new
                            {
                                type = "textarea",
                                name = "configuration",
                                label = "节点配置",
                                placeholder = "节点的额外配置（JSON格式）",
                                minRows = 3,
                                value = "{}"
                            }
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

