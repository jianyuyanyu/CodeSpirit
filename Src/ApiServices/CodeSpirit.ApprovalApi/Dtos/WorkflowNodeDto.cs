using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Attributes;
using Newtonsoft.Json;

namespace CodeSpirit.ApprovalApi.Dtos;

/// <summary>
/// 工作流节点查询DTO
/// </summary>
public class WorkflowNodeQueryDto : QueryDtoBase
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [DisplayName("工作流定义ID")]
    public long? WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [StringLength(100)]
    [DisplayName("节点名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    [DisplayName("节点类型")]
    public WorkflowNodeType? NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode? ApprovalMode { get; set; }
}

/// <summary>
/// 创建工作流节点DTO
/// </summary>
public class CreateWorkflowNodeDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "节点名称长度不能超过100个字符")]
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    [DisplayName("节点类型")]
    public WorkflowNodeType NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Sequential;

    /// <summary>
    /// 节点配置（JSON格式）
    /// </summary>
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批人配置
    /// </summary>
    [DisplayName("审批人配置")]
    public List<CreateWorkflowNodeApproverDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件配置
    /// </summary>
    [DisplayName("条件配置")]
    public List<CreateWorkflowNodeConditionDto> Conditions { get; set; } = new();
}

/// <summary>
/// 更新工作流节点DTO
/// </summary>
public class UpdateWorkflowNodeDto
{
    /// <summary>
    /// 节点名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "节点名称长度不能超过100个字符")]
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    [DisplayName("节点类型")]
    public WorkflowNodeType NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Sequential;

    /// <summary>
    /// 节点配置（JSON格式）
    /// </summary>
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批人配置
    /// </summary>
    [DisplayName("审批人配置")]
    public List<CreateWorkflowNodeApproverDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件配置
    /// </summary>
    [DisplayName("条件配置")]
    public List<CreateWorkflowNodeConditionDto> Conditions { get; set; } = new();
}

/// <summary>
/// 创建工作流节点审批人DTO
/// </summary>
public class CreateWorkflowNodeApproverDto
{
    /// <summary>
    /// 审批人类型
    /// </summary>
    [Required]
    [DisplayName("审批人类型")]
    public ApproverType ApproverType { get; set; }

    /// <summary>
    /// 审批人值
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "审批人值长度不能超过100个字符")]
    [DisplayName("审批人值")]
    public string ApproverValue { get; set; } = string.Empty;

    /// <summary>
    /// 审批人名称
    /// </summary>
    [StringLength(100, ErrorMessage = "审批人名称长度不能超过100个字符")]
    [DisplayName("审批人名称")]
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>
    /// 排序
    /// </summary>
    [DisplayName("排序")]
    public int Order { get; set; } = 0;
}

/// <summary>
/// 创建工作流节点条件DTO
/// </summary>
public class CreateWorkflowNodeConditionDto
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "条件表达式长度不能超过500个字符")]
    [DisplayName("条件表达式")]
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 下一个节点名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "下一个节点名称长度不能超过100个字符")]
    [DisplayName("下一个节点名称")]
    public string NextNodeName { get; set; } = string.Empty;

    /// <summary>
    /// 条件描述
    /// </summary>
    [StringLength(200, ErrorMessage = "条件描述长度不能超过200个字符")]
    [DisplayName("条件描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 排序
    /// </summary>
    [DisplayName("排序")]
    public int Order { get; set; } = 0;
}

/// <summary>
/// 批量创建工作流节点DTO
/// </summary>
public class BatchCreateWorkflowNodesDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 节点列表
    /// </summary>
    [Required]
    [DisplayName("节点列表")]
    public List<CreateWorkflowNodeDto> Nodes { get; set; } = new();
}

/// <summary>
/// 工作流流程设计DTO
/// </summary>
public class WorkflowProcessDesignDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    [Required]
    [DisplayName("工作流定义ID")]
    public long WorkflowDefinitionId { get; set; }

    /// <summary>
    /// 流程配置（包含节点和连线信息的JSON）
    /// </summary>
    [Required]
    [DisplayName("流程配置")]
    public string ProcessConfig { get; set; } = string.Empty;

    /// <summary>
    /// 节点列表
    /// </summary>
    [Required]
    [DisplayName("节点列表")]
    public List<CreateWorkflowNodeDto> Nodes { get; set; } = new();
}

/// <summary>
/// 工作流节点批量导入项DTO
/// </summary>
public class WorkflowNodeBatchImportItemDto
{
    /// <summary>
    /// 节点名称
    /// </summary>
    [Required]
    [JsonProperty("节点名称")]
    [DisplayName("节点名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [Required]
    [JsonProperty("节点类型")]
    [DisplayName("节点类型")]
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// 审批模式
    /// </summary>
    [JsonProperty("审批模式")]
    [DisplayName("审批模式")]
    public string ApprovalMode { get; set; } = string.Empty;

    /// <summary>
    /// 节点配置
    /// </summary>
    [JsonProperty("节点配置")]
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = "{}";
}
