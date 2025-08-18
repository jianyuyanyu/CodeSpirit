using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Dtos.ExamRecord;

/// <summary>
/// 完成考试DTO
/// </summary>
public class FinishExamDto
{
    /// <summary>
    /// 考试记录ID
    /// </summary>
    [Required(ErrorMessage = "考试记录ID不能为空")]
    [DisplayName("考试记录ID")]
    public long ExamRecordId { get; set; }
    
    /// <summary>
    /// 是否强制提交（即使有题目未答完）
    /// </summary>
    [DisplayName("是否强制提交")]
    public bool ForceSubmit { get; set; }
    
    /// <summary>
    /// 提交时间
    /// </summary>
    [DisplayName("提交时间")]
    public DateTime? SubmitTime { get; set; }
} 