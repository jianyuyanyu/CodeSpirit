using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

/// <summary>
/// 表单设计DTO
/// </summary>
[AiFormFill(
    TriggerField = nameof(BusinessScenario),
    UseIndependentLLM = true,
    MaxTokens = 3000,
    EnableCache = true,
    CacheExpirationMinutes = 30,
    GlobalFillPrompt = "根据选定的工作流和业务场景，智能设计符合AMIS规范的审批表单结构，自动包含审批流程相关的关键信息字段"
)]
[FormGroup("workflow", "工作流信息", "Name,Code,Description", Order = 1)]
[FormGroup("requirements", "需求描述", "BusinessScenario,RequiredFields,OptionalFields,SpecialFeatures", Order = 2)]
[FormGroup("formSchema", "表单设计", "FormSchema,FormSchemaReview", Order = 3, Direction = FormGroupDirection.Vertical)]
public class FormDesignDto
{
    /// <summary>
    /// 工作流名称（只读，用于显示）
    /// </summary>
    [DisplayName("工作流名称")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 6)]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码（只读，用于显示）
    /// </summary>
    [DisplayName("工作流代码")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 6)]
    public string? Code { get; set; }

    /// <summary>
    /// 工作流描述（只读，用于显示）
    /// </summary>
    [DisplayName("工作流描述")]
    [AmisFormField(Type = "static", VisibleOn = "${id}", ColumnRatio = 12)]
    public string? Description { get; set; }

    /// <summary>
    /// 业务场景描述
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "业务场景描述长度不能超过500个字符")]
    [DisplayName("业务场景描述")]
    [Description("详细描述表单的业务场景和用途，例如：员工请假申请、设备采购申请、合同审批申请等")]
    [AiFieldFill(Weight = 4, Priority = 1, CustomDescription = "根据选定的工作流和业务场景，生成符合审批流程的表单设计")]
    [AmisInputTextField(Placeholder = "请详细描述表单的业务场景和用途", ColumnRatio = 12)]
    public string BusinessScenario { get; set; } = string.Empty;

    /// <summary>
    /// 必需字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("必需字段要求")]
    [Description("描述表单中必须包含的字段和信息，例如：申请人信息、申请时间、申请原因、金额等")]
    [AiFieldFill(Weight = 3, Priority = 2, CustomDescription = "描述表单中必须包含的字段名称，例如：申请人信息、申请时间、申请原因、金额等")]
    [AmisTextareaField(Placeholder = "请描述表单必须包含的字段信息", ColumnRatio = 6)]
    public string? RequiredFields { get; set; }

    /// <summary>
    /// 可选字段要求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("可选字段要求")]
    [Description("描述表单中可选的字段和信息，例如：备注、附件、紧急程度等")]
    [AiFieldFill(Weight = 2, Priority = 3, CustomDescription = "描述表单中可选的字段和信息，例如：备注、附件、紧急程度等")]
    [AmisTextareaField(Placeholder = "请描述表单可选的字段信息", ColumnRatio = 6)]
    public string? OptionalFields { get; set; }

    /// <summary>
    /// 特殊功能需求
    /// </summary>
    [StringLength(1000)]
    [DisplayName("特殊功能需求")]
    [Description("表单的特殊功能需求，例如：文件上传、级联选择、条件显示、动态表格等")]
    [AiFieldFill(Weight = 2, Priority = 6)]
    [AmisTextareaField(Placeholder = "请描述表单需要的特殊功能", ColumnRatio = 12)]
    public string? SpecialFeatures { get; set; }

    /// <summary>
    /// 生成的表单Schema
    /// </summary>
    [DisplayName("表单定义")]
    [Description("AI生成的符合AMIS规范的审批表单JSON结构")]
    [AiFieldFill(Weight = 5, Priority = 7, CustomDescription = "根据工作流信息、业务场景、字段要求等信息，生成符合AMIS规范的完整审批表单Schema，自动包含审批相关的关键信息字段（申请人、申请时间、审批状态、当前节点、审批历史等）、表单字段、验证规则、布局配置，并使用antd主题样式。注意：不要生成审批历史列表、提交按钮，文件上传组件类型为input-file。")]
    [AmisFormField(Placeholder = "请输入符合AMIS规范的JSON Schema", Type = "editor", AdditionalConfig = "{\"language\":\"json\"}", ColumnRatio = 6)]
    public string? FormSchema { get; set; }

    [DisplayName("")]
    [AmisFormField(Type = "amis", Placeholder = "将根据表单设计要求自动生成", AdditionalConfig = "{\"name\":\"formSchema\"}", ColumnRatio = 6)]
    public string? FormSchemaReview { get; set; }
}
