using CodeSpirit.ApprovalApi.Models;
using Newtonsoft.Json.Converters;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

/// <summary>
/// 前端节点DTO
/// </summary>
public class FrontendNodeDto
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public WorkflowNodeType NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public ApprovalMode ApprovalMode { get; set; }

    /// <summary>
    /// 节点配置
    /// </summary>
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// 审批人列表
    /// </summary>
    public List<FrontendApproverDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件列表
    /// </summary>
    public List<FrontendConditionDto> Conditions { get; set; } = new();
}
