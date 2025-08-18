using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.QuestionCategory;

/// <summary>
/// 题目分类DTO
/// </summary>
public class QuestionCategoryDto
{
    /// <summary>
    /// ID
    /// </summary>
    [AmisColumn(Hidden = true)]
    public long Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [DisplayName("分类名称")]
    [TplColumn("<i class=\"${icon} mr-1\"></i> ${name}")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 图标（用于模板显示）
    /// </summary>
    [AmisColumn(Hidden = true)]
    public string Icon { get; set; } = "fa fa-folder";
    
    /// <summary>
    /// 分类描述
    /// </summary>
    [DisplayName("描述")]
    [AmisColumn(Type = "text", Remark = "分类的详细描述信息")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 父分类ID
    /// </summary>
    [AmisColumn(Hidden = true)]
    public long? ParentId { get; set; }
    
    /// <summary>
    /// 父分类名称
    /// </summary>
    [DisplayName("父分类")]
    [TplColumn("${parentName || '根分类'}")]
    [AmisColumn(Sortable = false)]
    public string? ParentName { get; set; }
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    [TplColumn("<span class=\"badge badge-info\">${questionCount}</span>")]
    public int QuestionCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 子分类列表（用于树形展示）
    /// </summary>
    [IgnoreColumn]
    [DisplayName("子分类")]
    public List<QuestionCategoryDto> Children { get; set; } = [];
} 