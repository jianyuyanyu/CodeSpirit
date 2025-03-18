using System.ComponentModel;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 错题查询DTO
/// </summary>
public class WrongQuestionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 考生ID
    /// </summary>
    [DisplayName("考生ID")]
    public long? StudentId { get; set; }
    
    /// <summary>
    /// 考试设置ID
    /// </summary>
    [DisplayName("考试设置ID")]
    public long? ExamSettingId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [DisplayName("题目ID")]
    public long? QuestionId { get; set; }
    
    /// <summary>
    /// 题目类型
    /// </summary>
    [DisplayName("题目类型")]
    public string QuestionType { get; set; }
    
    /// <summary>
    /// 考试时间范围
    /// </summary>
    [DisplayName("考试时间")]
    public DateTime[] ExamTimeRange { get; set; }
} 