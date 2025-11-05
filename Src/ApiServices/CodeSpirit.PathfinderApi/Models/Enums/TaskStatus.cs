namespace CodeSpirit.PathfinderApi.Models.Enums;

/// <summary>
/// 任务状态枚举
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 待办
    /// </summary>
    [Display(Name = "待办")]
    Pending = 1,
    
    /// <summary>
    /// 进行中
    /// </summary>
    [Display(Name = "进行中")]
    InProgress = 2,
    
    /// <summary>
    /// 已完成
    /// </summary>
    [Display(Name = "已完成")]
    Completed = 3,
    
    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 4
}

