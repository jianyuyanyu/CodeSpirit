using CodeSpirit.Shared.Entities.Interfaces;

namespace CodeSpirit.Shared.Entities;

/// <summary>
/// 包含完整审计信息的实体基类
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class AuditableEntityBase<TKey> : 
    EntityBase<TKey>,
    IFullAuditable
{
    /// <inheritdoc />
    public long CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public long? DeletedBy { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
