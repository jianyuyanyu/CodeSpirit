using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Dtos.Settings;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.IdentityApi.Services.Sms;

/// <summary>
/// 开发模式短信发送器（日志输出）
/// </summary>
public class DevelopmentSmsSender : ISmsSender, IScopedDependency
{
    private readonly ILogger<DevelopmentSmsSender> _logger;

    /// <summary>
    /// 初始化开发模式短信发送器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DevelopmentSmsSender(ILogger<DevelopmentSmsSender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 发送短信验证码（开发模式：仅输出日志）
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="settings">短信设置</param>
    /// <returns>是否发送成功</returns>
    public Task<bool> SendAsync(string phoneNumber, string code, SmsSettingsDto settings)
    {
        _logger.LogWarning(
            "【开发模式】短信验证码 - 手机号: {PhoneNumber}, 验证码: {Code}, 有效期: {ExpiresInSeconds}秒",
            phoneNumber, code, settings.CodeExpireSeconds);

        return Task.FromResult(true);
    }
}

