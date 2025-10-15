namespace CodeSpirit.Caching.Models;

/// <summary>
/// 缓存优先级枚举
/// </summary>
public enum CachePriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    Low = 1,

    /// <summary>
    /// 正常优先级
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 高优先级
    /// </summary>
    High = 3,

    /// <summary>
    /// 永不移除
    /// </summary>
    NeverRemove = 4
}
