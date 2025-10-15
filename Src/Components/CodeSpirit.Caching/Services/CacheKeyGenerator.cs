using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeSpirit.Caching.Services;

/// <summary>
/// 缓存键生成器实现
/// </summary>
public class CacheKeyGenerator : ICacheKeyGenerator
{
    private readonly CachingOptions _options;
    private static readonly Regex KeyValidationRegex = new(@"^[a-zA-Z0-9:_\-\.]+$", RegexOptions.Compiled);
    private const int MaxKeyLength = 250;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">缓存配置选项</param>
    public CacheKeyGenerator(IOptions<CachingOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    public string GenerateKey(string prefix, params object[] parts)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new ArgumentNullException(nameof(prefix));

        var keyBuilder = new StringBuilder();
        keyBuilder.Append(_options.KeyPrefix);
        keyBuilder.Append(prefix);

        if (parts != null && parts.Length > 0)
        {
            foreach (var part in parts)
            {
                if (part != null)
                {
                    keyBuilder.Append(':');
                    keyBuilder.Append(NormalizePart(part));
                }
            }
        }

        var key = keyBuilder.ToString();
        
        // 验证键长度
        if (key.Length > MaxKeyLength)
        {
            // 如果键太长，使用哈希值
            key = GenerateHashKey(key);
        }

        if (!ValidateKey(key))
        {
            throw new ArgumentException($"生成的缓存键格式无效: {key}");
        }

        return key;
    }

    /// <summary>
    /// 生成带租户的缓存键
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    public string GenerateTenantKey(string tenantId, string prefix, params object[] parts)
    {
        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentNullException(nameof(tenantId));

        var allParts = new List<object> { $"tenant:{tenantId}" };
        if (parts != null)
        {
            allParts.AddRange(parts);
        }

        return GenerateKey(prefix, allParts.ToArray());
    }

    /// <summary>
    /// 生成用户特定的缓存键
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="prefix">键前缀</param>
    /// <param name="parts">键组成部分</param>
    /// <returns>生成的缓存键</returns>
    public string GenerateUserKey(long userId, string prefix, params object[] parts)
    {
        var allParts = new List<object> { $"user:{userId}" };
        if (parts != null)
        {
            allParts.AddRange(parts);
        }

        return GenerateKey(prefix, allParts.ToArray());
    }

    /// <summary>
    /// 验证缓存键格式
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>如果格式有效返回true，否则返回false</returns>
    public bool ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (key.Length > MaxKeyLength)
            return false;

        return KeyValidationRegex.IsMatch(key);
    }

    /// <summary>
    /// 从缓存键中提取前缀
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>提取的前缀</returns>
    public string ExtractPrefix(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!key.StartsWith(_options.KeyPrefix))
            return string.Empty;

        var withoutGlobalPrefix = key.Substring(_options.KeyPrefix.Length);
        var firstColonIndex = withoutGlobalPrefix.IndexOf(':');
        
        return firstColonIndex > 0 
            ? withoutGlobalPrefix.Substring(0, firstColonIndex)
            : withoutGlobalPrefix;
    }

    /// <summary>
    /// 规范化键组成部分
    /// </summary>
    /// <param name="part">键组成部分</param>
    /// <returns>规范化后的字符串</returns>
    private static string NormalizePart(object part)
    {
        return part switch
        {
            string str => str.Replace(":", "_").Replace(" ", "_"),
            DateTime dt => dt.ToString("yyyyMMddHHmmss"),
            DateTimeOffset dto => dto.ToString("yyyyMMddHHmmss"),
            Guid guid => guid.ToString("N"),
            _ => part.ToString()?.Replace(":", "_").Replace(" ", "_") ?? string.Empty
        };
    }

    /// <summary>
    /// 生成哈希键（当原始键太长时使用）
    /// </summary>
    /// <param name="originalKey">原始键</param>
    /// <returns>哈希键</returns>
    private string GenerateHashKey(string originalKey)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalKey));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
        
        // 保留前缀以便识别
        var prefix = ExtractPrefix(originalKey);
        return $"{_options.KeyPrefix}{prefix}:hash:{hashString}";
    }
}
