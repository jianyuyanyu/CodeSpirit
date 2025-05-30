using CodeSpirit.Core;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 学生-分组映射关系
/// </summary>
public class StudentGroupMapping : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 学生ID
    /// </summary>
    [Required]
    public long StudentId { get; set; }

    /// <summary>
    /// 学生
    /// </summary>
    public Student Student { get; set; } = null!;

    /// <summary>
    /// 分组ID
    /// </summary>
    [Required]
    public long StudentGroupId { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    public StudentGroup StudentGroup { get; set; } = null!;

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
} 