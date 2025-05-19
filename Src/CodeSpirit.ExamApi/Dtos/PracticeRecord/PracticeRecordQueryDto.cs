using System.ComponentModel;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ExamApi.Data.Models.Enums;

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
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public QuestionType? QuestionType { get; set; }
    
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
    /// 模拟考试ID
    /// </summary>
    [DisplayName("模拟考试ID")]
    public long? MockExamId { get; set; }
    
    /// <summary>
    /// 练习设置ID
    /// </summary>
    [DisplayName("练习设置ID")]
    public long? PracticeSettingId { get; set; }
    
    /// <summary>
    /// 试卷ID
    /// </summary>
    [DisplayName("试卷ID")]
    public long? ExamPaperId { get; set; }
    
    /// <summary>
    /// 练习开始时间
    /// </summary>
    [DisplayName("练习开始时间")]
    public DateTime? PracticeStartTime { get; set; }
    
    /// <summary>
    /// 练习结束时间
    /// </summary>
    [DisplayName("练习结束时间")]
    public DateTime? PracticeEndTime { get; set; }
} 