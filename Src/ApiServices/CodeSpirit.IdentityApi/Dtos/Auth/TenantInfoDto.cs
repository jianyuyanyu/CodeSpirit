using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Auth;

/// <summary>
/// 租户信息DTO
/// </summary>
public class TenantInfoDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [DisplayName("租户名称")]
    public string TenantName { get; set; } = string.Empty;
}
