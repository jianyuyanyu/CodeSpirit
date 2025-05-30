using CodeSpirit.ExamApi.Data.Models.Enums;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Core;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 练习会话
/// </summary>
public class PracticeSession : LongKeyAuditableEntityBase, IMultiTenant
{
    /// <summary>
    /// 学生ID
    /// </summary>
    public long StudentId { get; set; }

    /// <summary>
    /// 学生
    /// </summary>
    public Student Student { get; set; } = null!;

    /// <summary>
    /// 练习设置ID
    /// </summary>
    public long PracticeSettingId { get; set; }

    /// <summary>
    /// 练习设置
    /// </summary>
    public PracticeSetting PracticeSetting { get; set; } = null!;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 正确题目数量
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public PracticeSessionStatus Status { get; set; }

    /// <summary>
    /// 练习记录
    /// </summary>
    public List<PracticeRecord> PracticeRecords { get; set; } = new List<PracticeRecord>();

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
} 