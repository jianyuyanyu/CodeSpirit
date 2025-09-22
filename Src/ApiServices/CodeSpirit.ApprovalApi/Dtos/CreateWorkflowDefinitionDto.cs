using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 创建工作流定义DTO
/// </summary>
[DisplayName("创建工作流定义")]
[AiFormFill(
    TriggerField = nameof(Name), 
    UseIndependentLLM = true,
    MaxTokens = 2000,
    EnableCache = true,
    CacheExpirationMinutes = 60,
    IgnoreFields = new[] { nameof(CustomPrompt), nameof(Configuration) }
)]
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
    [Description("具体的业务应用场景，例如：员工请假、设备采购、供应商合同签署、财务报销、项目审批等")]
    [AiFieldFill(Weight = 3, Priority = 4, CustomDescription = "详细的业务应用场景，用于生成针对性的审批流程和表单结构")]
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
    [Description("描述需要的条件分支逻辑，例如：金额大于1万需要财务总监审批、请假天数超过3天需要部门经理和HR审批等")]
    [AiFieldFill(Weight = 3, Priority = 7, CustomDescription = "条件分支的具体业务规则，用于生成带有条件判断的工作流节点配置")]
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
    [AmisFormField(Type = "amis", Placeholder = "将根据工作流信息自动生成")]
    public string? FormSchema { get; set; }

    /// <summary>
    /// 审批角色和层级
    /// </summary>
    [StringLength(500)]
    [DisplayName("审批角色和层级")]
    [Description("描述各级审批人的角色或职位，例如：直接主管→部门经理→总监→总经理")]
    [AiFieldFill(Weight = 3, Priority = 9, CustomDescription = "根据业务场景和审批层级生成具体的审批角色配置，包含各级审批人的职位或角色名称")]
    [AmisTextareaField(Placeholder = "请描述各级审批人的角色，例如：直接主管→部门经理→总监")]
    public string? ApprovalRoles { get; set; }

    /// <summary>
    /// 工作流节点Schema（JSON格式的节点配置）
    /// </summary>
    [DisplayName("工作流节点Schema")]
    [Description("工作流的节点配置，包含各个审批节点的配置信息，JSON格式。节点包含：名称、类型（Start/Approval/Condition/End）、审批模式、审批人配置、条件配置等")]
    [AiFieldFill(Weight = 3, Priority = 10, CustomDescription = "根据工作流名称、业务场景、预期审批层级、审批角色和条件分支描述，生成完整的工作流节点配置JSON数组。每个节点对象需包含：name(节点名称，使用中文命名)、nodeType(节点类型：Start/Approval/Condition/End)、approvalMode(审批模式：Sequential/Parallel/CounterSign/OrSign)、configuration(节点配置JSON字符串)、approvers(审批人数组，包含approverType和approverValue)、conditions(条件数组，包含expression和nextNodeName)。确保至少包含一个Start节点和一个End节点")]
    [AmisFormField(Type = "json", Placeholder = "将根据工作流信息自动生成")]
    public string? WorkflowNodeSchema { get; set; }

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
    [AmisSelectField(
        Source = "${ROOT_API}/api/approval/WorkflowCategories/enabled",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false,
        Clearable = true
    )]
    [DisplayName("工作流分类")]
    [Description("选择工作流所属的分类")]
    public int? CategoryId { get; set; }
}
