namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 短信验证码服务接口
/// </summary>
public interface ISmsCodeService
{
    /// <summary>
    /// 生成并发送验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendCodeAsync(string phoneNumber, string tenantId);

    /// <summary>
    /// 验证验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>是否验证通过</returns>
    Task<bool> VerifyCodeAsync(string phoneNumber, string code, string tenantId);
}

