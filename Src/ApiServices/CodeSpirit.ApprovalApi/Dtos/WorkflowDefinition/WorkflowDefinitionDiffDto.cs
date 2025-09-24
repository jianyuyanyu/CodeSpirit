using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Attributes;
using CodeSpirit.ApprovalApi.Models;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowDefinition;

/// <summary>
/// 工作流定义差异DTO
/// </summary>
public class WorkflowDefinitionDiffDto
{
    /// <summary>
    /// 工作流定义ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工作流名称
    /// </summary>
    [StringLength(100, ErrorMessage = "工作流名称长度不能超过100个字符")]
    [DisplayName("工作流名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 工作流代码
    /// </summary>
    [StringLength(50, ErrorMessage = "工作流代码长度不能超过50个字符")]
    [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "工作流代码必须以大写字母开头，只能包含大写字母、数字和下划线")]
    [DisplayName("工作流代码")]
    public string? Code { get; set; }

    /// <summary>
    /// 工作流描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    [DisplayName("描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 工作流配置
    /// </summary>
    [DisplayName("工作流配置")]
    public string? Configuration { get; set; }

    /// <summary>
    /// 审批表单Schema
    /// </summary>
    [DisplayName("审批表单Schema")]
    public string? FormSchema { get; set; }
}
