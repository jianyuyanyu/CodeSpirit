using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Models;

namespace CodeSpirit.IdentityApi.Services.ThirdParty;

/// <summary>
/// 第三方API服务工厂
/// 根据平台类型返回对应的API服务实现
/// </summary>
public class ThirdPartyApiServiceFactory : IThirdPartyApiService
{
    private readonly WeChatApiService _weChatApiService;
    private readonly ILogger<ThirdPartyApiServiceFactory> _logger;
    
    public ThirdPartyApiServiceFactory(
        WeChatApiService weChatApiService,
        ILogger<ThirdPartyApiServiceFactory> logger)
    {
        _weChatApiService = weChatApiService;
        _logger = logger;
    }
    
    public async Task<ThirdPartySessionInfo> GetSessionAsync(
        ThirdPartyPlatformType platformType, 
        string credential, 
        ThirdPartyPlatformConfig config)
    {
        return platformType switch
        {
            ThirdPartyPlatformType.WeChatMiniProgram => 
                await _weChatApiService.GetSessionAsync(platformType, credential, config),
            
            ThirdPartyPlatformType.AlipayMiniProgram => 
                throw new NotImplementedException("支付宝小程序登录暂未实现"),
            
            _ => throw new NotSupportedException($"不支持的平台类型: {platformType}")
        };
    }
}

