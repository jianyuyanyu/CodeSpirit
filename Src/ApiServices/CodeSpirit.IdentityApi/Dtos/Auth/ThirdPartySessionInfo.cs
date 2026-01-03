namespace CodeSpirit.IdentityApi.Dtos.Auth;

/// <summary>
/// 第三方会话信息
/// </summary>
public class ThirdPartySessionInfo
{
    /// <summary>
    /// 平台OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;
    
    /// <summary>
    /// 平台UnionId（可选）
    /// </summary>
    public string? UnionId { get; set; }
    
    /// <summary>
    /// 会话密钥
    /// </summary>
    public string SessionKey { get; set; } = string.Empty;
}

/// <summary>
/// 第三方平台配置
/// </summary>
public class ThirdPartyPlatformConfig
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}

