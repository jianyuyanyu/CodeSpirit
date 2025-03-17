namespace CodeSpirit.ExamApi.Data.Models;

/// <summary>
/// 考试状态
/// </summary>
public enum ExamStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    NotStarted = 1,
    
    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 2,
    
    /// <summary>
    /// 已结束
    /// </summary>
    Finished = 3
}
