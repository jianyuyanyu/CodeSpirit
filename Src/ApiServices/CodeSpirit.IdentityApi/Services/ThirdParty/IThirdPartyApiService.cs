using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Models;

namespace CodeSpirit.IdentityApi.Services.ThirdParty;

/// <summary>
/// 第三方平台API服务接口
/// </summary>
public interface IThirdPartyApiService
{
    /// <summary>
    /// 通过登录凭证获取用户会话信息
    /// </summary>
    /// <param name="platformType">平台类型</param>
    /// <param name="credential">登录凭证</param>
    /// <param name="config">平台配置</param>
    /// <returns>会话信息</returns>
    Task<ThirdPartySessionInfo> GetSessionAsync(
        ThirdPartyPlatformType platformType, 
        string credential, 
        ThirdPartyPlatformConfig config);
}

