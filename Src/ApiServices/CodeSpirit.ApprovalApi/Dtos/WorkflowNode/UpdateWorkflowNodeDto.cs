using CodeSpirit.ApprovalApi.Models;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

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
