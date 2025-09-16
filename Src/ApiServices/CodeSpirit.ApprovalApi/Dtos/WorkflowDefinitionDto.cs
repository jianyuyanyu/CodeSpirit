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