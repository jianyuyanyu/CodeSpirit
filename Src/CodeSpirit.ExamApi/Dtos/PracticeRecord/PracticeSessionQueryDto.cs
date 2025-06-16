using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习会话查询DTO
/// </summary>
public class PracticeSessionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 学生ID
    /// </summary>
    [Display(Name = "学生ID")]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// 练习ID
    /// </summary>
    [Display(Name = "练习ID")]
    public long? PracticeId { get; set; }
    
    /// <summary>
    /// 状态筛选
    /// </summary>
    [Display(Name = "状态")]
    public string? Status { get; set; }
    
    /// <summary>
    /// 开始时间范围（起始）
    /// </summary>
    [Display(Name = "开始时间起始")]
    public DateTime? StartTimeBegin { get; set; }
    
    /// <summary>
    /// 开始时间范围（结束）
    /// </summary>
    [Display(Name = "开始时间结束")]
    public DateTime? StartTimeEnd { get; set; }
} 