using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.Core;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 题目分类
/// </summary>
public class QuestionCategory : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 父分类ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 父分类
    /// </summary>
    public QuestionCategory? Parent { get; set; }

    /// <summary>
    /// 子分类列表
    /// </summary>
    public List<QuestionCategory> Children { get; set; } = [];

    /// <summary>
    /// 题目列表
    /// </summary>
    public List<Question> Questions { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
}