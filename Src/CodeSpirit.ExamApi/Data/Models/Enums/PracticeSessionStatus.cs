namespace CodeSpirit.ExamApi.Data.Models.Enums;

/// <summary>
/// 练习会话状态
/// </summary>
public enum PracticeSessionStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 0,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// 已中断
    /// </summary>
    Interrupted = 2
} 