namespace CodeSpirit.Shared.Entities;

/// <summary>
/// 使用long类型作为主键的审计实体基类（建议使用雪花Id）
/// </summary>
public abstract class LongKeyAuditableEntityBase : AuditableEntityBase<long>
{ } 