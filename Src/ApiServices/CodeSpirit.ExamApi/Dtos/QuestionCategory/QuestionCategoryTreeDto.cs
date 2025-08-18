using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Extensions;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.QuestionCategory;

/// <summary>
/// 题目分类树形结构DTO
/// </summary>
public class QuestionCategoryTreeDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [DisplayName("名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父级分类ID
    /// </summary>
    [DisplayName("父级分类")]
    public long? ParentId { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    [DisplayName("显示顺序")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [DisplayName("备注")]
    public string? Description { get; set; }

    /// <summary>
    /// 子分类
    /// </summary>
    [DisplayName("子分类")]
    public List<QuestionCategoryTreeDto> Children { get; set; } = [];
} 