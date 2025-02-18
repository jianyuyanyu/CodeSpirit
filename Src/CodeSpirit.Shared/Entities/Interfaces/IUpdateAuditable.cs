namespace CodeSpirit.Shared.Entities.Interfaces;

/// <summary>
/// 定义更新审计信息的接口
/// </summary>
public interface IUpdateAuditable
{
    /// <summary>
    /// 获取或设置最后更新人ID
    /// </summary>
    long? UpdatedBy { get; set; }

    /// <summary>
    /// 获取或设置最后更新时间
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}