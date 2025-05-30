using CodeSpirit.Core;
using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试设置-学生分组关联实体
/// </summary>
public class ExamSettingStudentGroup : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 考试设置ID
    /// </summary>
    public long ExamSettingId { get; set; }

    /// <summary>
    /// 考试设置
    /// </summary>
    public ExamSetting ExamSetting { get; set; } = null!;

    /// <summary>
    /// 学生分组ID
    /// </summary>
    public long StudentGroupId { get; set; }

    /// <summary>
    /// 学生分组
    /// </summary>
    public StudentGroup StudentGroup { get; set; } = null!;

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
} 