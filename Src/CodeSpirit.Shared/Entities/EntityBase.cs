namespace CodeSpirit.Shared.Entities;

/// <summary>
/// 实体基类，提供泛型主键支持
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class EntityBase<TKey>
{
    /// <summary>
    /// 获取或设置实体主键
    /// </summary>
    public TKey Id { get; set; }
} 