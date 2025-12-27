namespace CodeSpirit.Localization.Services;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 获取当前语言代码
    /// </summary>
    /// <returns>当前语言代码</returns>
    Task<string> GetCurrentLanguageAsync();

    /// <summary>
    /// 设置用户语言偏好
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="language">语言代码</param>
    /// <returns>操作结果</returns>
    Task<bool> SetUserLanguageAsync(string userId, string language);

    /// <summary>
    /// 设置租户默认语言
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="language">语言代码</param>
    /// <returns>操作结果</returns>
    Task<bool> SetTenantLanguageAsync(string tenantId, string language);

    /// <summary>
    /// 设置全局默认语言
    /// </summary>
    /// <param name="language">语言代码</param>
    /// <returns>操作结果</returns>
    Task<bool> SetGlobalLanguageAsync(string language);
}
