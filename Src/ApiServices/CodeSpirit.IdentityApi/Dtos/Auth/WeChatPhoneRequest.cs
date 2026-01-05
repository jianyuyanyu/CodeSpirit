using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth;

/// <summary>
/// 微信手机号获取请求模型
/// </summary>
public class WeChatPhoneRequest
{
    /// <summary>
    /// 微信手机号授权码（从 getPhoneNumber 回调中获取）
    /// </summary>
    [Required(ErrorMessage = "授权码不能为空")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 微信手机号获取结果
/// </summary>
public class WeChatPhoneResult
{
    /// <summary>
    /// 手机号
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 国家代码（如：86）
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 纯手机号（不含国家代码）
    /// </summary>
    public string PurePhoneNumber { get; set; } = string.Empty;
}

