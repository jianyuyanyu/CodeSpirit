using CodeSpirit.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考生分组实体
/// </summary>
public class StudentGroup: LongKeyAuditableEntityBase
{    
    /// <summary>
    /// 分组名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 分组描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 分组下的考生
    /// </summary>
    public ICollection<StudentGroupMapping> Students { get; set; } = new List<StudentGroupMapping>();
    
    /// <summary>
    /// 分组参与的考试
    /// </summary>
    public ICollection<ExamSetting> ExamSettings { get; set; } = new List<ExamSetting>();
}