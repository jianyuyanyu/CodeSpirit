using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 微信登录设置DTO
/// </summary>
public class WeChatLoginSettingsDto
{
    /// <summary>
    /// 微信小程序AppId
    /// </summary>
    [DisplayName("微信小程序AppId")]
    [StringLength(100)]
    [AmisInputTextField(Label = "微信小程序AppId", Placeholder = "请输入微信小程序AppId")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 微信小程序AppSecret
    /// </summary>
    [DisplayName("微信小程序AppSecret")]
    [StringLength(200)]
    [AmisFormFieldAttribute("input-password", Label = "微信小程序AppSecret", Placeholder = "请输入微信小程序AppSecret")]
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用微信登录
    /// </summary>
    [DisplayName("启用微信登录")]
    [AmisSwitchField(Label = "启用微信登录")]
    public bool Enabled { get; set; } = false;
}

