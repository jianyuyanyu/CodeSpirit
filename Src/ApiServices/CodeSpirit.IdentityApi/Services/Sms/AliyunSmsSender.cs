using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.IdentityApi.Dtos.Settings;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.IdentityApi.Services.Sms;

/// <summary>
/// 阿里云短信发送器
/// </summary>
public class AliyunSmsSender : ISmsSender, IScopedDependency
{
    private readonly ILogger<AliyunSmsSender> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 初始化阿里云短信发送器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="httpClientFactory">HTTP客户端工厂</param>
    public AliyunSmsSender(
        ILogger<AliyunSmsSender> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 发送短信验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="settings">短信设置</param>
    /// <returns>是否发送成功</returns>
    public async Task<bool> SendAsync(string phoneNumber, string code, SmsSettingsDto settings)
    {
        try
        {
            if (string.IsNullOrEmpty(settings.SecretId) || string.IsNullOrEmpty(settings.SecretKey))
            {
                _logger.LogError("阿里云短信配置不完整：AccessKeyId或AccessKeySecret为空");
                return false;
            }

            // TODO: 实现阿里云短信API调用
            // 这里需要集成阿里云SDK或调用阿里云API
            // 参考文档：https://help.aliyun.com/document_detail/101414.html

            _logger.LogWarning(
                "阿里云短信发送功能待实现 - 手机号: {PhoneNumber}, 验证码: {Code}, SignName: {SignName}, TemplateId: {TemplateId}",
                phoneNumber, code, settings.SignName, settings.TemplateId);

            // 临时返回true，实际使用时需要实现真实的API调用
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云短信发送失败：手机号 {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}

