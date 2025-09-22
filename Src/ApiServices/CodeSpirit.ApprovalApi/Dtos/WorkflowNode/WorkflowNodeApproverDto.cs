using CodeSpirit.ApprovalApi.Models;
using Newtonsoft.Json.Converters;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

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
    [JsonConverter(typeof(StringEnumConverter))]
    public ApproverType ApproverType { get; set; }

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
