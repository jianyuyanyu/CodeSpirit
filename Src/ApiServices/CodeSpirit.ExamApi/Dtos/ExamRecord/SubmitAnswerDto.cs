using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 提交答案DTO
/// </summary>
public class SubmitAnswerDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Required(ErrorMessage = "考试记录ID不能为空")]
    [DisplayName("考试记录ID")]
    public long ExamRecordId { get; set; }
    
    /// <summary>
    /// 题目ID
    /// </summary>
    [Required(ErrorMessage = "题目ID不能为空")]
    [DisplayName("题目ID")]
    public long QuestionId { get; set; }
    
    /// <summary>
    /// 题目版本ID
    /// </summary>
    [Required(ErrorMessage = "题目版本ID不能为空")]
    [DisplayName("题目版本ID")]
    public long QuestionVersionId { get; set; }
    
    /// <summary>
    /// 答案内容
    /// </summary>
    [DisplayName("答案内容")]
    [StringLength(2000)]
    public string Answer { get; set; }
    
    /// <summary>
    /// 是否标记（考生标记的疑难题目）
    /// </summary>
    [DisplayName("是否标记")]
    public bool IsMarked { get; set; }
    
    /// <summary>
    /// 答题开始时间
    /// </summary>
    [DisplayName("答题开始时间")]
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 答题提交时间
    /// </summary>
    [DisplayName("答题提交时间")]
    public DateTime? SubmitTime { get; set; }
} 