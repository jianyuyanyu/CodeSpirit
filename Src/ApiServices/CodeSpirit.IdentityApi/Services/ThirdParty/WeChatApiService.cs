using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Models;
using SKIT.FlurlHttpClient.Wechat.Api;
using SKIT.FlurlHttpClient.Wechat.Api.Models;

namespace CodeSpirit.IdentityApi.Services.ThirdParty;

/// <summary>
/// 微信API服务实现（基于SKIT.FlurlHttpClient.Wechat）
/// </summary>
public class WeChatApiService : IThirdPartyApiService
{
    private readonly ILogger<WeChatApiService> _logger;
    
    public WeChatApiService(ILogger<WeChatApiService> logger)
    {
        _logger = logger;
    }
    
    public virtual async Task<ThirdPartySessionInfo> GetSessionAsync(
        ThirdPartyPlatformType platformType, 
        string credential, 
        ThirdPartyPlatformConfig config)
    {
        if (platformType != ThirdPartyPlatformType.WeChatMiniProgram)
        {
            throw new ArgumentException($"不支持的平台类型: {platformType}");
        }
        
        try
        {
            // 创建微信API客户端
            var client = new WechatApiClient(new WechatApiClientOptions
            {
                AppId = config.AppId,
                AppSecret = config.AppSecret
            });
            
            // 调用jscode2session接口
            var request = new SnsJsCode2SessionRequest
            {
                JsCode = credential
            };
            
            var response = await client.ExecuteSnsJsCode2SessionAsync(request);
            
            // 检查响应是否成功
            if (!response.IsSuccessful())
            {
                _logger.LogError("微信登录API调用失败: {ErrorCode} - {ErrorMessage}", 
                    response.ErrorCode, response.ErrorMessage);
                throw new InvalidOperationException($"微信登录失败: {response.ErrorMessage}");
            }
            
            // 转换为通用会话信息
            return new ThirdPartySessionInfo
            {
                OpenId = response.OpenId ?? string.Empty,
                UnionId = response.UnionId, // 可能为空
                SessionKey = response.SessionKey ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用微信API异常");
            throw;
        }
    }
    
    /// <summary>
    /// 获取微信手机号（基于 code）
    /// </summary>
    /// <param name="code">手机号授权码</param>
    /// <param name="config">平台配置</param>
    /// <returns>手机号信息</returns>
    public virtual async Task<WeChatPhoneResult> GetPhoneNumberAsync(string code, ThirdPartyPlatformConfig config)
    {
        try
        {
            // 创建微信API客户端
            var client = new WechatApiClient(new WechatApiClientOptions
            {
                AppId = config.AppId,
                AppSecret = config.AppSecret
            });
            
            // 调用获取手机号接口（新版基于 code）
            var request = new WxaBusinessGetUserPhoneNumberRequest
            {
                Code = code
            };
            
            var response = await client.ExecuteWxaBusinessGetUserPhoneNumberAsync(request);
            
            // 检查响应是否成功
            if (!response.IsSuccessful())
            {
                _logger.LogError("获取微信手机号失败: {ErrorCode} - {ErrorMessage}", 
                    response.ErrorCode, response.ErrorMessage);
                throw new InvalidOperationException($"获取手机号失败: {response.ErrorMessage}");
            }
            
            // 转换为结果
            var phoneInfo = response.PhoneInfo;
            if (phoneInfo == null)
            {
                throw new InvalidOperationException("手机号信息为空");
            }
            
            return new WeChatPhoneResult
            {
                PhoneNumber = phoneInfo.PhoneNumber ?? string.Empty,
                CountryCode = phoneInfo.CountryCode ?? "86",
                PurePhoneNumber = phoneInfo.PurePhoneNumber ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取微信手机号异常");
            throw;
        }
    }
}

