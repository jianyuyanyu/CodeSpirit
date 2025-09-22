using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ApprovalApi.Dtos.WorkflowCategory;

/// <summary>
/// 流程分类DTO
/// </summary>
public class WorkflowCategoryDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [DisplayName("分类ID")]
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [DisplayName("分类名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    [DisplayName("分类描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 分类颜色
    /// </summary>
    [DisplayName("分类颜色")]
    public string? Color { get; set; }

    /// <summary>
    /// 分类图标
    /// </summary>
    [DisplayName("分类图标")]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    [DisplayName("排序")]
    public int OrderIndex { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("启用状态")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类ID")]
    [AmisColumn(Hidden = true)]
    public int? ParentId { get; set; }

    /// <summary>
    /// 父级分类名称
    /// </summary>
    [DisplayName("父级分类")]
    [AmisColumn(Hidden = true)]
    public string? ParentName { get; set; }

    /// <summary>
    /// 子分类列表
    /// </summary>
    [DisplayName("子分类")]
    [IgnoreColumn]
    public List<WorkflowCategoryDto> Children { get; set; } = new();

    /// <summary>
    /// 流程数量
    /// </summary>
    [DisplayName("流程数量")]
    public int WorkflowCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [DisplayName("创建人")]
    [AggregateField(dataSource: "http://identity/api/identity/internal/users/{value}.data.name", template: "{field}")]
    public long CreatedBy { get; set; }
}
