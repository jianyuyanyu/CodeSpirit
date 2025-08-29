using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.SurveyApi.Models;

/// <summary>
/// 问卷分类实体
/// </summary>
public class SurveyCategory : AuditableEntityBase<int>, IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 分类名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 分类颜色（十六进制颜色值）
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// 分类图标
    /// </summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 父级分类ID（支持层级分类）
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 父级分类
    /// </summary>
    public virtual SurveyCategory? Parent { get; set; }

    /// <summary>
    /// 子级分类集合
    /// </summary>
    public virtual ICollection<SurveyCategory> Children { get; set; } = new List<SurveyCategory>();

    /// <summary>
    /// 该分类下的问卷集合
    /// </summary>
    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
