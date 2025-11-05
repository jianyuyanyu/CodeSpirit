namespace CodeSpirit.PathfinderApi.Models.Enums;

/// <summary>
/// 目标状态枚举
/// </summary>
public enum GoalStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    [Display(Name = "进行中")]
    Active = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    [Display(Name = "已完成")]
    Completed = 2,
    
    /// <summary>
    /// 已暂停
    /// </summary>
    [Display(Name = "已暂停")]
    Paused = 3,
    
    /// <summary>
    /// 已取消
    /// </summary>
    [Display(Name = "已取消")]
    Cancelled = 4
}

