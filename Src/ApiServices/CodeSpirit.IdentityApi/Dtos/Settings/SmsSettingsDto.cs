using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.Settings.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Settings;

/// <summary>
/// 短信服务提供商枚举
/// </summary>
public enum SmsProvider
{
    /// <summary>
    /// 无（开发模式）
    /// </summary>
    [Display(Name = "无（开发模式）")]
    None = 0,

    /// <summary>
    /// 腾讯云短信
    /// </summary>
    [Display(Name = "腾讯云短信")]
    TencentCloud = 1,

    /// <summary>
    /// 阿里云短信
    /// </summary>
    [Display(Name = "阿里云短信")]
    Aliyun = 2
}

/// <summary>
/// 短信服务设置DTO
/// </summary>
[SettingsDto("Auth", "SmsSettings")]
public class SmsSettingsDto
{
    /// <summary>
    /// 是否启用短信验证码登录
    /// </summary>
    [DisplayName("启用短信验证码登录")]
    [AmisSwitchField(Label = "启用短信验证码登录")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 短信服务提供商
    /// </summary>
    [DisplayName("短信服务提供商")]
    [AmisSelectField(
        Label = "短信服务提供商",
        Options = "None:无（开发模式）,TencentCloud:腾讯云短信,Aliyun:阿里云短信")]
    public SmsProvider Provider { get; set; } = SmsProvider.None;

    /// <summary>
    /// 腾讯云SecretId / 阿里云AccessKeyId
    /// </summary>
    [DisplayName("SecretId/AccessKeyId")]
    [StringLength(200)]
    [AmisInputTextField(Label = "SecretId/AccessKeyId", Placeholder = "腾讯云SecretId或阿里云AccessKeyId")]
    public string SecretId { get; set; } = string.Empty;

    /// <summary>
    /// 腾讯云SecretKey / 阿里云AccessKeySecret
    /// </summary>
    [DisplayName("SecretKey/AccessKeySecret")]
    [StringLength(200)]
    [AmisFormFieldAttribute("input-password", Label = "SecretKey/AccessKeySecret", Placeholder = "腾讯云SecretKey或阿里云AccessKeySecret")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 腾讯云SdkAppId（仅腾讯云需要）
    /// </summary>
    [DisplayName("腾讯云SdkAppId")]
    [StringLength(50)]
    [AmisInputTextField(Label = "腾讯云SdkAppId", Placeholder = "仅腾讯云需要，阿里云可留空")]
    public string SdkAppId { get; set; } = string.Empty;

    /// <summary>
    /// 短信签名
    /// </summary>
    [DisplayName("短信签名")]
    [StringLength(50)]
    [AmisInputTextField(Label = "短信签名", Placeholder = "请输入短信签名")]
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 验证码模板ID
    /// </summary>
    [DisplayName("验证码模板ID")]
    [StringLength(50)]
    [AmisInputTextField(Label = "验证码模板ID", Placeholder = "请输入验证码模板ID")]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// 验证码长度
    /// </summary>
    [DisplayName("验证码长度")]
    [Range(4, 8)]
    [AmisNumberField(Label = "验证码长度", Min = 4, Max = 8, DefaultValue = 6)]
    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    [DisplayName("验证码有效期（秒）")]
    [Range(60, 600)]
    [AmisNumberField(Label = "验证码有效期（秒）", Min = 60, Max = 600, DefaultValue = 300)]
    public int CodeExpireSeconds { get; set; } = 300;

    /// <summary>
    /// 发送间隔（秒）
    /// </summary>
    [DisplayName("发送间隔（秒）")]
    [Range(10, 300)]
    [AmisNumberField(Label = "发送间隔（秒）", Min = 10, Max = 300, DefaultValue = 60)]
    public int SendIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用超级验证码
    /// </summary>
    [DisplayName("启用超级验证码")]
    [AmisSwitchField(Label = "启用超级验证码（开发/测试环境使用）")]
    public bool EnableSuperCode { get; set; } = false;

    /// <summary>
    /// 超级验证码（始终有效，用于开发测试）
    /// </summary>
    [DisplayName("超级验证码")]
    [StringLength(10)]
    [AmisInputTextField(Label = "超级验证码", Placeholder = "开发/测试环境使用的万能验证码", DefaultValue = "000000")]
    public string SuperCode { get; set; } = "000000";
}

