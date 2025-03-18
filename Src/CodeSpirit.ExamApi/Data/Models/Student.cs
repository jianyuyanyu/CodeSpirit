using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考生实体
/// </summary>
public class Student : LongKeyAuditableEntityBase, IIsActive
{    
    /// <summary>
    /// 用户ID（关联到身份系统）
    /// </summary>
    [Required]
    public long UserId { get; set; } 
    
    /// <summary>
    /// 考生姓名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 考生学号/工号
    /// </summary>
    [Required]
    [StringLength(50)]
    public string StudentNumber { get; set; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    [Required]
    [StringLength(20)]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 考生所属分组
    /// </summary>
    public ICollection<StudentGroupMapping> StudentGroups { get; set; } = new List<StudentGroupMapping>();
    
    /// <summary>
    /// 练习记录
    /// </summary>
    public ICollection<PracticeRecord> PracticeRecords { get; set; } = new List<PracticeRecord>();
    
    /// <summary>
    /// 错题
    /// </summary>
    public ICollection<WrongQuestion> WrongQuestions { get; set; } = new List<WrongQuestion>();
    
    /// <summary>
    /// 考试记录
    /// </summary>
    public ICollection<ExamRecord> ExamRecords { get; set; } = new List<ExamRecord>();

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }
}