using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

/// <summary>
/// 表单设计DTO
/// </summary>
[DisplayName("表单设计")]
[AiFormFill(
    TriggerField = nameof(BusinessScenario),
    UseIndependentLLM = true,
    MaxTokens = 3000,
    EnableCache = true,
    CacheExpirationMinutes = 30,
    GlobalFillPrompt = "使用AI智能设计表单结构",
    IgnoreFields = new[] { nameof(CustomPrompt) }
)]
public class FormDesignDto
{
    /// <summary>
    /// 业务场景描述
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "业务场景描述长度不能超过500个字符")]
    [DisplayName("业务场景描述")]
    [Description("详细描述表单的业务场景和用途，例如：员工请假申请、设备采购申请、合同审批申请等")]
    [AiFieldFill(Weight = 3, Priority = 1)]
    [AmisTextareaField(Placeholder = "请详细描述表单的业务场景和用途")]
    public string BusinessScenario { get; set; } = string.Empty;

    /// <summary>
    /// 表单类型
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("表单类型")]
    [Description("表单的业务类型，如：申请表单、审批表单、信息收集表单等")]
    [AiFieldFill(Weight = 2, Priority = 2)]
    [AmisSelectField(
        Clearable = true,
        AdditionalConfig = "{\"options\":[{\"label\":\"申请表单\",\"value\":\"申请表单\"},{\"label\":\"审批表单\",\"value\":\"审批表单\"},{\"label\":\"信息收集表单\",\"value\":\"信息收集表单\"},{\"label\":\"反馈表单\",\"value\":\"反馈表单\"},{\"label\":\"登记表单\",\"value\":\"登记表单\"},{\"label\":\"评估表单\",\"value\":\"评估表单\"}]}"
    )]
    public string FormType { get; set; } = string.Empty;

    /// <summary>
    /// 目标用户
    /// </summary>
    [StringLength(200)]
    [DisplayName("目标用户")]
    [Description("表单的主要使用者，例如：员工、管理员、客户、供应商等")]
    [AiFieldFill(Weight = 1, Priority = 3)]
    [AmisInputTextField(Placeholder = "例如：员工、管理员、客户")]
    public string? TargetUsers { get; set; }

    /// <summary>
    /// 必需字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("必需字段要求")]
    [Description("描述表单中必须包含的字段和信息，例如：申请人信息、申请时间、申请原因、金额等")]
    [AiFieldFill(Weight = 3, Priority = 4, CustomDescription = "根据业务场景生成必要的表单字段配置，包含字段名称、类型、验证规则等")]
    [AmisTextareaField(Placeholder = "请描述表单必须包含的字段信息")]
    public string? RequiredFields { get; set; }

    /// <summary>
    /// 可选字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("可选字段要求")]
    [Description("描述表单中可选的字段和信息，例如：备注、附件、紧急程度等")]
    [AiFieldFill(Weight = 2, Priority = 5)]
    [AmisTextareaField(Placeholder = "请描述表单可选的字段信息")]
    public string? OptionalFields { get; set; }

    /// <summary>
    /// 验证规则要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("验证规则要求")]
    [Description("描述表单字段的验证规则，例如：金额范围、时间限制、格式要求等")]
    [AiFieldFill(Weight = 2, Priority = 6)]
    [AmisTextareaField(Placeholder = "请描述字段的验证规则要求")]
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 界面布局偏好
    /// </summary>
    [StringLength(200)]
    [DisplayName("界面布局偏好")]
    [Description("表单的布局偏好，例如：单列布局、双列布局、分组布局、卡片布局等")]
    [AiFieldFill(Weight = 1, Priority = 7)]
    [AmisSelectField(
        Clearable = true,
        AdditionalConfig = "{\"options\":[{\"label\":\"单列布局\",\"value\":\"单列布局\"},{\"label\":\"双列布局\",\"value\":\"双列布局\"},{\"label\":\"分组布局\",\"value\":\"分组布局\"},{\"label\":\"卡片布局\",\"value\":\"卡片布局\"},{\"label\":\"标签页布局\",\"value\":\"标签页布局\"}]}"
    )]
    public string? LayoutPreference { get; set; }

    /// <summary>
    /// 特殊功能需求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("特殊功能需求")]
    [Description("表单的特殊功能需求，例如：文件上传、级联选择、条件显示、动态表格等")]
    [AiFieldFill(Weight = 2, Priority = 8)]
    [AmisTextareaField(Placeholder = "请描述表单需要的特殊功能")]
    public string? SpecialFeatures { get; set; }

    /// <summary>
    /// 自定义提示词
    /// </summary>
    [StringLength(2000)]
    [DisplayName("自定义提示词")]
    [Description("可选：提供自定义的AI提示词来指导表单生成，留空则使用默认提示词")]
    [AiFieldFill(Enabled = false)]
    [AmisTextareaField(Placeholder = "请输入自定义提示词（可选）")]
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// 生成的表单Schema
    /// </summary>
    [DisplayName("表单Schema")]
    [Description("AI生成的符合AMIS规范的表单JSON结构")]
    [AiFieldFill(Weight = 5, Priority = 9, CustomDescription = "根据业务场景、表单类型、字段要求等信息，生成符合AMIS规范的完整表单Schema，包含所有必需和可选字段、验证规则、布局配置等")]
    [AmisFormField(Type = "amis", Placeholder = "将根据表单设计要求自动生成")]
    public string? FormSchema { get; set; }

    /// <summary>
    /// 使用antd主题
    /// </summary>
    [DisplayName("使用antd主题")]
    [Description("是否使用antd主题样式")]
    [AmisFormField(Type = "switch")]
    public bool UseAntdTheme { get; set; } = true;

    /// <summary>
    /// 是否包含提交按钮
    /// </summary>
    [DisplayName("包含提交按钮")]
    [Description("生成的表单是否包含提交、重置等操作按钮")]
    [AmisFormField(Type = "switch")]
    public bool IncludeSubmitButtons { get; set; } = true;

    /// <summary>
    /// 表单标题
    /// </summary>
    [StringLength(100)]
    [DisplayName("表单标题")]
    [Description("表单的显示标题")]
    [AiFieldFill(Weight = 1, Priority = 10)]
    [AmisInputTextField(Placeholder = "请输入表单标题")]
    public string? FormTitle { get; set; }

    /// <summary>
    /// 包含审批关键信息
    /// </summary>
    [DisplayName("包含审批关键信息")]
    [Description("是否在表单中包含审批相关的关键信息（只读显示）")]
    [AmisFormField(Type = "switch")]
    public bool IncludeApprovalContext { get; set; } = true;

    /// <summary>
    /// 审批关键信息配置
    /// </summary>
    [StringLength(1000)]
    [DisplayName("审批关键信息")]
    [Description("需要在表单中显示的审批关键信息，例如：申请人、申请时间、当前审批节点、审批历史等")]
    [AiFieldFill(Weight = 2, Priority = 11, CustomDescription = "根据业务场景生成需要在审批表单中显示的关键信息字段，包括申请人信息、申请时间、审批状态、当前节点、审批历史等只读信息")]
    [AmisTextareaField(Placeholder = "请描述需要显示的审批关键信息", VisibleOn = "${IncludeApprovalContext}")]
    public string? ApprovalContextFields { get; set; }

    /// <summary>
    /// 工作流定义ID（用于关联）
    /// </summary>
    [DisplayName("关联工作流")]
    [Description("选择要关联的工作流定义")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/approval/WorkflowDefinitions",
        ValueField = "id",
        LabelField = "name",
        Clearable = true,
        Searchable = true
    )]
    public long? WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 工作流名称（只读，用于显示）
    /// </summary>
    [DisplayName("工作流名称")]
    [Description("关联的工作流名称")]
    [AmisFormField(Type = "static", VisibleOn = "${WorkflowDefinitionId}")]
    public string? WorkflowName { get; set; }

    /// <summary>
    /// 工作流描述（只读，用于显示）
    /// </summary>
    [DisplayName("工作流描述")]
    [Description("关联的工作流描述信息")]
    [AmisFormField(Type = "static", VisibleOn = "${WorkflowDefinitionId}")]
    public string? WorkflowDescription { get; set; }
}
