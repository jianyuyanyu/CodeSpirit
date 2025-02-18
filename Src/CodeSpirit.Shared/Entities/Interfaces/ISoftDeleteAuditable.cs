namespace CodeSpirit.Shared.Entities.Interfaces;

/// <summary>
/// 定义软删除审计信息的接口
/// </summary>
public interface ISoftDeleteAuditable : IDeletionAuditable
{
    /// <summary>
    /// 获取或设置是否已删除
    /// </summary>
    bool IsDeleted { get; set; }
}