using System.Reflection;

namespace CodeSpirit.Shared.EventBus.Events;

/// <summary>
/// 用户删除
/// </summary>
public class UserDeletedEvent
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }
} 