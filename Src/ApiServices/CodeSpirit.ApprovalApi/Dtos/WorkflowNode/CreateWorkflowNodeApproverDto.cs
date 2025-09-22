using CodeSpirit.ApprovalApi.Models;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

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
