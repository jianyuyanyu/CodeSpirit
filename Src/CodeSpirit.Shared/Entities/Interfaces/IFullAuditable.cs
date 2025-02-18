namespace CodeSpirit.Shared.Entities.Interfaces;

/// <summary>
/// 完整的审计接口，包含创建、更新和软删除审计信息
/// </summary>
public interface IFullAuditable :
    ICreationAuditable,
    IUpdateAuditable,
    ISoftDeleteAuditable
{ }