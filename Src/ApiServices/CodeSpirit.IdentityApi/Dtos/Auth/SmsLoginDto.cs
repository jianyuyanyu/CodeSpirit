using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.Auth;

/// <summary>
/// 发送短信验证码请求
/// </summary>
public class SendSmsCodeRequest
{
    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// 短信验证码登录请求
/// </summary>
public class SmsLoginRequest
{
    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(10, MinimumLength = 4, ErrorMessage = "验证码长度不正确")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// 发送短信验证码响应
/// </summary>
public class SendSmsCodeResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }
}

