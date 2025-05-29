namespace CodeSpirit.Core;

/// <summary>
/// 多租户实体接口
/// 实现此接口的实体将自动支持多租户数据隔离
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    string TenantId { get; set; }
} 