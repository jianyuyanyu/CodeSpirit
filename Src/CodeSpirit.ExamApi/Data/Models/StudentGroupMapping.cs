using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 学生-分组映射关系
/// </summary>
public class StudentGroupMapping : AuditableEntityBase<int>
{
    /// <summary>
    /// 学生ID
    /// </summary>
    [Required]
    public int StudentId { get; set; }

    /// <summary>
    /// 学生
    /// </summary>
    public Student Student { get; set; } = null!;

    /// <summary>
    /// 分组ID
    /// </summary>
    [Required]
    public int StudentGroupId { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    public StudentGroup StudentGroup { get; set; } = null!;
} 