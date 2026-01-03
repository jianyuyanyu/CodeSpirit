using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 通知设置DTO
/// </summary>
public class NotificationSettingsDto
{
    /// <summary>
    /// 启用邮件通知
    /// </summary>
    [DisplayName("启用邮件通知")]
    [AmisSwitchField(Label = "启用邮件通知")]
    public bool EnableEmailNotification { get; set; } = true;
    
    /// <summary>
    /// 启用短信通知
    /// </summary>
    [DisplayName("启用短信通知")]
    [AmisSwitchField(Label = "启用短信通知")]
    public bool EnableSmsNotification { get; set; } = false;
    
    /// <summary>
    /// 启用站内消息
    /// </summary>
    [DisplayName("启用站内消息")]
    [AmisSwitchField(Label = "启用站内消息")]
    public bool EnableInAppNotification { get; set; } = true;
}

