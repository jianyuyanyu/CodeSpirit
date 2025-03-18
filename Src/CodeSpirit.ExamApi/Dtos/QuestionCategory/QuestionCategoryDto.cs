using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.Columns;

namespace CodeSpirit.ExamApi.Dtos.QuestionCategory;

/// <summary>
/// 题目分类DTO
/// </summary>
public class QuestionCategoryDto
{
    /// <summary>
    /// ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [DisplayName("分类名称")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分类描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 父分类ID
    /// </summary>
    [DisplayName("父分类ID")]
    public long? ParentId { get; set; }
    
    /// <summary>
    /// 父分类名称
    /// </summary>
    [DisplayName("父分类")]
    public string? ParentName { get; set; }
    
    /// <summary>
    /// 题目数量
    /// </summary>
    [DisplayName("题目数量")]
    public int QuestionCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [DisplayName("更新人")]
    public string? UpdatedBy { get; set; }
} 