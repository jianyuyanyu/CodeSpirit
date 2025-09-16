using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Core.Attributes;
using Elastic.Clients.Elasticsearch.TextStructure;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 工作流定义DTO
/// </summary>
public class WorkflowDefinitionDto
{
    /// <summary>
    /// 工作流ID
    /// </summary>
    [DisplayName("工作流ID")]
    public long Id { get; set; }

    /// <summary>
    /// 工作流名称
    /// </summary>
    [DisplayName("工作流名称")]
    [AmisColumn]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码
    /// </summary>
    [DisplayName("工作流代码")]
    [AmisColumn]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工作流描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 工作流版本
    /// </summary>
    [DisplayName("版本")]
    [AmisColumn]
    public int Version { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    [AmisColumn(Type = "switch")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [AmisColumn(Type = "datetime")]
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 工作流定义查询DTO
/// </summary>
public class WorkflowDefinitionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 工作流名称（模糊查询）
    /// </summary>
    [DisplayName("工作流名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码（模糊查询）
    /// </summary>
    [DisplayName("工作流代码")]
    public string? Code { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    [DisplayName("版本")]
    public int? Version { get; set; }
}

/// <summary>
/// 创建工作流定义DTO
/// </summary>
[DisplayName("创建工作流定义")]
[AiFormFill(TriggerField = nameof(Name), UseIndependentLLM = true, 
    CustomPromptTemplate = "基于工作流名称和业务场景，生成完整的工作流定义信息。请确保生成的FormSchema符合AMIS规范，包含必要的表单字段和验证规则。")]
public class CreateWorkflowDefinitionDto
{
    /// <summary>
    /// 工作流名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "工作流名称长度不能超过100个字符")]
    [DisplayName("工作流名称")]
    [Description("请输入工作流名称，例如：请假审批流程、采购申请审批、合同审批等")]
    [AmisInputTextField(Placeholder = "请输入工作流名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码（唯一标识）
    /// </summary>
    [Required]
    [StringLength(50, ErrorMessage = "工作流代码长度不能超过50个字符")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "工作流代码必须以大写字母开头，只能包含大写字母、数字和下划线")]
    [DisplayName("工作流代码")]
    [Description("工作流的唯一标识代码，用于系统内部识别，建议使用英文大写字母和下划线")]
    [AiFieldFill(Weight = 2, Priority = 1)]
    [AmisInputTextField(Placeholder = "例如：LEAVE_APPROVAL")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工作流描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    [DisplayName("工作流描述")]
    [Description("详细描述工作流的用途、适用场景和业务规则")]
    [AiFieldFill(Weight = 2, Priority = 2)]
    [AmisTextareaField(Placeholder = "请详细描述工作流的用途和适用场景")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 工作流类型
    /// </summary>
    [StringLength(100)]
    [DisplayName("工作流类型")]
    [Description("工作流的业务类型，如：人事审批、财务审批、采购审批、合同审批等")]
    [AiFieldFill(Weight = 1, Priority = 3)]
    [AmisFormField(Type = "input-text", Placeholder = "请输入工作流类型")]
    public string? WorkflowType { get; set; }

    /// <summary>
    /// 业务场景
    /// </summary>
    [StringLength(200)]
    [DisplayName("业务场景")]
    [Description("具体的业务应用场景，帮助AI更好地生成相关的表单字段")]
    [AiFieldFill(Weight = 2, Priority = 4)]
    [AmisFormField(Type = "input-text", Placeholder = "例如：员工请假、设备采购、供应商合同签署")]
    public string? BusinessScenario { get; set; }

    /// <summary>
    /// 预期审批层级
    /// </summary>
    [Range(1, 10)]
    [DisplayName("预期审批层级")]
    [Description("预期的审批层级数量，用于生成合适的审批流程")]
    [AiFieldFill(Weight = 1, Priority = 5)]
    [AmisNumberField(DefaultValue = 2, Min = 1, Max = 10)]
    public int ExpectedApprovalLevels { get; set; } = 2;

    /// <summary>
    /// 是否需要条件分支
    /// </summary>
    [DisplayName("是否需要条件分支")]
    [Description("是否需要根据业务条件进行不同的审批路径")]
    [AiFieldFill(Weight = 1, Priority = 6)]
    [AmisFormField(Type = "switch")]
    public bool RequireConditionalBranch { get; set; } = false;

    /// <summary>
    /// 条件分支描述
    /// </summary>
    [StringLength(1000)]
    [DisplayName("条件分支描述")]
    [Description("描述需要的条件分支逻辑，例如：金额大于1万需要财务总监审批")]
    [AiFieldFill(Weight = 2, Priority = 7)]
    [AmisTextareaField(Placeholder = "请描述条件分支的具体逻辑", VisibleOn = "${RequireConditionalBranch}")]
    public string? ConditionalBranchDescription { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    [Description("创建后是否立即启用此工作流")]
    [AmisFormField(Type = "switch")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 工作流配置（JSON格式）
    /// </summary>
    [DisplayName("工作流配置")]
    [Description("高级配置选项，JSON格式，包含超时时间、提醒设置等")]
    [AiFieldFill(Enabled = false)]
    [AmisFormField(Type = "json", Placeholder = "高级配置选项（可选）")]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批表单Schema（符合AMIS要求的JSON结构）
    /// </summary>
    [DisplayName("审批表单Schema")]
    [Description("审批时显示的表单结构，符合AMIS规范的JSON格式")]
    [AiFieldFill(Weight = 3, Priority = 8, CustomDescription = "根据工作流名称、业务场景和描述，生成符合AMIS规范的表单Schema，包含必要的静态信息展示、审批意见输入、审批结果选择等字段")]
    [AmisFormField(Type = "json", Placeholder = "将根据工作流信息自动生成")]
    public string? FormSchema { get; set; }

    /// <summary>
    /// 自定义提示词
    /// </summary>
    [StringLength(4000)]
    [DisplayName("自定义提示词")]
    [Description("可选：提供自定义的AI提示词来指导工作流和表单生成，留空则使用默认提示词")]
    [AiFieldFill(Enabled = false)]
    [AmisTextareaField(Placeholder = "请输入自定义提示词（可选）")]
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// 工作流分类ID
    /// </summary>
    [AmisInputTreeField(
        DataSource = "${ROOT_API}/api/approval/WorkflowCategories/tree",
        Multiple = false,
        JoinValues = true,
        ExtractValue = false,
        ShowOutline = true,
        LabelField = "name",
        ValueField = "id",
        Required = false,
        ShowIcon = true,
        Clearable = true,
        HeightAuto = true,
        SelectFirst = true
    )]
    [DisplayName("工作流分类")]
    [Description("选择工作流所属的分类")]
    public int? CategoryId { get; set; }
}

/// <summary>
/// 更新工作流定义DTO
/// </summary>
public class UpdateWorkflowDefinitionDto
{
    /// <summary>
    /// 工作流名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "工作流名称长度不能超过100个字符")]
    [DisplayName("工作流名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作流代码（唯一标识）
    /// </summary>
    [Required]
    [StringLength(50, ErrorMessage = "工作流代码长度不能超过50个字符")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "工作流代码必须以大写字母开头，只能包含大写字母、数字和下划线")]
    [DisplayName("工作流代码")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 工作流描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    [DisplayName("描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 工作流配置（JSON格式）
    /// </summary>
    [DisplayName("工作流配置")]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批表单Schema（符合AMIS要求的JSON结构）
    /// </summary>
    [DisplayName("审批表单Schema")]
    public string? FormSchema { get; set; }
}

/// <summary>
/// 工作流定义详情DTO
/// </summary>
public class WorkflowDefinitionDetailDto : WorkflowDefinitionDto
{
    /// <summary>
    /// 工作流配置
    /// </summary>
    [DisplayName("工作流配置")]
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// 审批表单Schema
    /// </summary>
    [DisplayName("审批表单Schema")]
    public string? FormSchema { get; set; }

    /// <summary>
    /// 工作流节点列表
    /// </summary>
    [DisplayName("工作流节点列表")]
    public List<WorkflowNodeDto> Nodes { get; set; } = new();
}

/// <summary>
/// 工作流节点DTO
/// </summary>
public class WorkflowNodeDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    [DisplayName("节点ID")]
    public long Id { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [DisplayName("节点类型")]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public string ApprovalMode { get; set; } = string.Empty;

    /// <summary>
    /// 节点配置
    /// </summary>
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// 审批人配置
    /// </summary>
    [DisplayName("审批人配置")]
    public List<WorkflowNodeApproverDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件配置
    /// </summary>
    [DisplayName("条件配置")]
    public List<WorkflowNodeConditionDto> Conditions { get; set; } = new();
}

/// <summary>
/// 工作流节点审批人DTO
/// </summary>
public class WorkflowNodeApproverDto
{
    /// <summary>
    /// 审批人ID
    /// </summary>
    [DisplayName("审批人ID")]
    public long Id { get; set; }

    /// <summary>
    /// 审批人类型
    /// </summary>
    [DisplayName("审批人类型")]
    public string ApproverType { get; set; } = string.Empty;

    /// <summary>
    /// 审批人值
    /// </summary>
    [DisplayName("审批人值")]
    public string ApproverValue { get; set; } = string.Empty;

    /// <summary>
    /// 审批人名称
    /// </summary>
    [DisplayName("审批人名称")]
    public string ApproverName { get; set; } = string.Empty;
}

/// <summary>
/// 工作流节点条件DTO
/// </summary>
public class WorkflowNodeConditionDto
{
    /// <summary>
    /// 条件ID
    /// </summary>
    [DisplayName("条件ID")]
    public long Id { get; set; }

    /// <summary>
    /// 条件表达式
    /// </summary>
    [DisplayName("条件表达式")]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 下一个节点名称
    /// </summary>
    [DisplayName("下一个节点名称")]
    public string NextNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 条件描述
    /// </summary>
    [DisplayName("条件描述")]
    public string Description { get; set; } = string.Empty;
}