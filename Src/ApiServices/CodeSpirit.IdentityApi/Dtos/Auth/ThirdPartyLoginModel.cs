using System.ComponentModel.DataAnnotations;
using CodeSpirit.IdentityApi.Models;

namespace CodeSpirit.IdentityApi.Dtos.Auth;

/// <summary>
/// 第三方登录请求模型
/// </summary>
public class ThirdPartyLoginModel
{
    /// <summary>
    /// 平台类型
    /// </summary>
    [Required]
    public ThirdPartyPlatformType PlatformType { get; set; }
    
    /// <summary>
    /// 登录凭证（微信为code，支付宝为authCode等）
    /// </summary>
    [Required]
    public string Credential { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// 微信登录请求模型（兼容性，内部转换为ThirdPartyLoginModel）
/// </summary>
public class WeChatLoginModel
{
    /// <summary>
    /// 微信登录code
    /// </summary>
    [Required]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;
}

