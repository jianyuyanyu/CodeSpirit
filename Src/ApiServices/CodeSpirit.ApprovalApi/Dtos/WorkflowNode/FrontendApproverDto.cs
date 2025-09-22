using CodeSpirit.ApprovalApi.Models;
using Newtonsoft.Json.Converters;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 前端审批人DTO
/// </summary>
public class FrontendApproverDto
{
    /// <summary>
    /// 审批人类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public ApproverType Type { get; set; }

    /// <summary>
    /// 审批人值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 审批人名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
