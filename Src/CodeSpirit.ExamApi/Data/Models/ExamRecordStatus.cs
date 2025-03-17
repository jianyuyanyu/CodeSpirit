namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试记录状态
/// </summary>
public enum ExamRecordStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// 已提交
    /// </summary>
    Submitted = 2,
    
    /// <summary>
    /// 已批改
    /// </summary>
    Graded = 3
}
