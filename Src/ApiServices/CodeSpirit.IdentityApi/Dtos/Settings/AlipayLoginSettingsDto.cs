using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Settings.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 支付宝登录设置DTO
/// </summary>
[SettingsDto("ThirdPartyLogin", "Alipay")]
public class AlipayLoginSettingsDto
{
    /// <summary>
    /// 支付宝小程序AppId
    /// </summary>
    [DisplayName("支付宝小程序AppId")]
    [StringLength(100)]
    [AmisInputTextField(Label = "支付宝小程序AppId", Placeholder = "请输入支付宝小程序AppId")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝小程序AppSecret
    /// </summary>
    [DisplayName("支付宝小程序AppSecret")]
    [StringLength(200)]
    [AmisFormFieldAttribute("input-password", Label = "支付宝小程序AppSecret", Placeholder = "请输入支付宝小程序AppSecret")]
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// 支付宝公钥（可选）
    /// </summary>
    [DisplayName("支付宝公钥")]
    [StringLength(500)]
    [AmisTextareaField(Label = "支付宝公钥", Placeholder = "请输入支付宝公钥", MinRows = 3)]
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用支付宝登录
    /// </summary>
    [DisplayName("启用支付宝登录")]
    [AmisSwitchField(Label = "启用支付宝登录")]
    public bool Enabled { get; set; } = false;
}

