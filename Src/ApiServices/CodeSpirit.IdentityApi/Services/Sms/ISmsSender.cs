using CodeSpirit.IdentityApi.Dtos.Settings;

namespace CodeSpirit.IdentityApi.Services.Sms;

/// <summary>
/// 短信发送接口
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// 发送短信验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="settings">短信设置</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendAsync(string phoneNumber, string code, SmsSettingsDto settings);
}

