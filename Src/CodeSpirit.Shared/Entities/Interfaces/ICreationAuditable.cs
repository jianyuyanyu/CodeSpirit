namespace CodeSpirit.Shared.Entities.Interfaces;

/// <summary>
/// 定义创建审计信息的接口
/// </summary>
public interface ICreationAuditable
{
    /// <summary>
    /// 获取或设置创建人ID
    /// </summary>
    long CreatedBy { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    DateTime CreatedAt { get; set; }
}