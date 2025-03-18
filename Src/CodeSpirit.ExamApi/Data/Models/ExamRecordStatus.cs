using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试记录状态
/// </summary>
public enum ExamRecordStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    [Display(Name = "进行中")]
    InProgress = 1,
    
    /// <summary>
    /// 已提交
    /// </summary>
    [Display(Name = "已提交")]
    Submitted = 2,
    
    /// <summary>
    /// 已批改
    /// </summary>
    [Display(Name = "已批改")]
    Graded = 3
}
