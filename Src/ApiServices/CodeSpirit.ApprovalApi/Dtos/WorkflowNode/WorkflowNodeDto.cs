using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.ApprovalApi.Models;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Attributes;
using Newtonsoft.Json;


namespace CodeSpirit.ApprovalApi.Dtos.WorkflowNode;

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
    public WorkflowNodeType NodeType { get; set; }

    /// <summary>
    /// 审批模式
    /// </summary>
    [DisplayName("审批模式")]
    public ApprovalMode ApprovalMode { get; set; }

    /// <summary>
    /// 节点配置
    /// </summary>
    [DisplayName("节点配置")]
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// 审批人配置
    /// </summary>
    [DisplayName("审批人配置")]
    [EachColumn(
        ItemTemplate = @"
            <span class='label label-info m-l-sm'>
                ${item.approverType} ：${item.approverName}(${item.approverValue})</span>
            </span>"
    )]
    public List<WorkflowNodeApproverDto> Approvers { get; set; } = new();

    /// <summary>
    /// 条件配置
    /// </summary>
    [DisplayName("条件配置")]
    [EachColumn(
        ItemTemplate = @"
            <span class='label label-warning m-l-sm'>
                ${item.expression} → ${item.nextNodeName}</span>
            </span>"
    )]
    public List<WorkflowNodeConditionDto> Conditions { get; set; } = new();
}