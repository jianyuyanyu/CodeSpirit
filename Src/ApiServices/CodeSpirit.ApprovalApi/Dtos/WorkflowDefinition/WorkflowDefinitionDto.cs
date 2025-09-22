using CodeSpirit.Core.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using Elastic.Clients.Elasticsearch.TextStructure;
using CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

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
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    [AmisColumn(Hidden = true)]
    public string TenantId { get; set; } = string.Empty;

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
    public int Version { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 流程分类ID
    /// </summary>
    [DisplayName("流程分类ID")]
    [AmisColumn(Hidden = true)]
    public int? CategoryId { get; set; }

    /// <summary>
    /// 流程分类名称
    /// </summary>
    [DisplayName("流程分类")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
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
    public int? CategoryId { get; set; }
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