namespace CodeSpirit.Caching.Abstractions;

/// <summary>
/// 缓存键生成器接口
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    string GenerateKey(string prefix, params object[] parts);

    /// <summary>
    /// 生成带租户的缓存键
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    string GenerateTenantKey(string tenantId, string prefix, params object[] parts);

    /// <summary>
    /// 生成用户特定的缓存键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    string GenerateUserKey(long userId, string prefix, params object[] parts);

    /// <summary>
    /// 验证缓存键格式
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>如果格式有效返回true，否则返回false</returns>
    bool ValidateKey(string key);

    /// <summary>
    /// 从缓存键中提取前缀
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>提取的前缀</returns>
    string ExtractPrefix(string key);
}
