using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 第三方登录设置DTO
/// </summary>
public class ThirdPartyLoginSettingsDto
{
    /// <summary>
    /// 微信小程序AppId
    /// </summary>
    [Display(Name = "微信小程序AppId")]
    [StringLength(100)]
    public string WeChatAppId { get; set; } = string.Empty;

    /// <summary>
    /// 微信小程序AppSecret
    /// </summary>
    [Display(Name = "微信小程序AppSecret")]
    [StringLength(200)]
    public string WeChatAppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝小程序AppId
    /// </summary>
    [Display(Name = "支付宝小程序AppId")]
    [StringLength(100)]
    public string AlipayAppId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝小程序AppSecret
    /// </summary>
    [Display(Name = "支付宝小程序AppSecret")]
    [StringLength(200)]
    public string AlipayAppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝公钥（可选）
    /// </summary>
    [Display(Name = "支付宝公钥")]
    [StringLength(500)]
    public string AlipayPublicKey { get; set; } = string.Empty;
}

