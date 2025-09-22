using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;

/// <summary>
/// 流程分类查询DTO
/// </summary>
public class WorkflowCategoryQueryDto : QueryDtoBase
{
    /// <summary>
    /// 分类名称（模糊搜索）
    /// </summary>
    [DisplayName("分类名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("启用状态")]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类ID")]
    public int? ParentId { get; set; }

    /// <summary>
    /// 只显示顶级分类
    /// </summary>
    [DisplayName("只显示顶级分类")]
    public bool? OnlyTopLevel { get; set; }
}
