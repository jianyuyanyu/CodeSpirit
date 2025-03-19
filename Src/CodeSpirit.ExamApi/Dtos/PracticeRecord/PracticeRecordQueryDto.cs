using System.ComponentModel.DataAnnotations;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models;

namespace CodeSpirit.ExamApi.Dtos.PracticeRecord;

/// <summary>
/// 练习记录查询DTO
/// </summary>
public class PracticeRecordQueryDto : QueryDtoBase
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long? QuestionId { get; set; }
    
    /// <summary>
    /// 练习类型
    /// </summary>
    [DisplayName("练习类型")]
    public PracticeType? PracticeType { get; set; }
    
    /// <summary>
    /// 是否正确
    /// </summary>
    [DisplayName("是否正确")]
    public bool? IsCorrect { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? EndTime { get; set; }
} 