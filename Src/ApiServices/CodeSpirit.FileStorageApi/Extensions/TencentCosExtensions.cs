using CodeSpirit.FileStorageApi.Options;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Bucket;
using COSXML.CosException;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Extensions;

/// <summary>
/// 腾讯云COS扩展方法
/// </summary>
public static class TencentCosExtensions
{
    /// <summary>
    /// 添加腾讯云COS服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTencentCos(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置
        services.Configure<TencentCosOptions>(configuration.GetSection(TencentCosOptions.SectionName));
        
        // 验证配置
        services.AddSingleton<IValidateOptions<TencentCosOptions>, TencentCosOptionsValidator>();
        
        // 注册COS服务
        services.AddSingleton<CosXml>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TencentCosOptions>>().Value;
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("TencentCosExtensions");
            
            return CreateCosXmlService(options, logger);
        });
        
        return services;
    }

    /// <summary>
    /// 创建COS服务实例
    /// </summary>
    /// <param name="options">COS配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>COS服务实例</returns>
    public static CosXml CreateCosXmlService(TencentCosOptions options, ILogger logger)
    {
        try
        {
            // 初始化配置
            var config = new CosXmlConfig.Builder()
                .IsHttps(options.UseHttps)
                .SetRegion(options.Region)
                .SetDebugLog(options.EnableDebugLog)
                .SetConnectionTimeoutMs(options.ConnectionTimeoutMs)
                .SetReadWriteTimeoutMs(options.ReadWriteTimeoutMs)
                .Build();

            // 注意：新版本SDK中已移除SetHost方法，服务域名通过Region自动设置

            // 创建凭证提供程序
            QCloudCredentialProvider credentialProvider;
            
            if (options.UseTemporaryCredentials && options.TemporaryCredentials != null)
            {
                // 使用临时密钥
                credentialProvider = new TencentCosTemporaryCredentialProvider(options, logger);
            }
            else
            {
                // 使用永久密钥
                credentialProvider = new DefaultQCloudCredentialProvider(
                    options.SecretId, 
                    options.SecretKey, 
                    options.SignatureDurationSeconds);
            }

            logger.LogInformation("腾讯云COS服务初始化成功，地域: {Region}", options.Region);
            return new CosXmlServer(config, credentialProvider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "腾讯云COS服务初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 生成存储桶的完整名称
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="appId">应用ID</param>
    /// <returns>完整的存储桶名称</returns>
    public static string GetFullBucketName(this string bucketName, string appId)
    {
        if (string.IsNullOrEmpty(bucketName))
        {
            throw new ArgumentException("存储桶名称不能为空", nameof(bucketName));
        }

        if (string.IsNullOrEmpty(appId))
        {
            throw new ArgumentException("AppId不能为空", nameof(appId));
        }

        // 如果已经包含APPID，直接返回
        if (bucketName.Contains("-") && bucketName.EndsWith(appId))
        {
            return bucketName;
        }

        return $"{bucketName}-{appId}";
    }

    /// <summary>
    /// 从完整的存储桶名称中提取存储桶名称
    /// </summary>
    /// <param name="fullBucketName">完整的存储桶名称</param>
    /// <returns>存储桶名称</returns>
    public static string ExtractBucketName(this string fullBucketName)
    {
        if (string.IsNullOrEmpty(fullBucketName))
        {
            return fullBucketName;
        }

        var lastDashIndex = fullBucketName.LastIndexOf('-');
        if (lastDashIndex > 0)
        {
            return fullBucketName[..lastDashIndex];
        }

        return fullBucketName;
    }

    /// <summary>
    /// 验证存储桶名称格式
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>是否有效</returns>
    public static bool IsValidBucketName(this string bucketName)
    {
        if (string.IsNullOrEmpty(bucketName))
        {
            return false;
        }

        // 存储桶名称长度为1-40个字符
        if (bucketName.Length < 1 || bucketName.Length > 40)
        {
            return false;
        }

        // 存储桶名称只能包含小写字母、数字、短划线(-)
        foreach (char c in bucketName)
        {
            if (!char.IsLower(c) && !char.IsDigit(c) && c != '-')
            {
                return false;
            }
        }

        // 存储桶名称不能以短划线开头或结尾
        if (bucketName.StartsWith('-') || bucketName.EndsWith('-'))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 验证对象键格式
    /// </summary>
    /// <param name="objectKey">对象键</param>
    /// <returns>是否有效</returns>
    public static bool IsValidObjectKey(this string objectKey)
    {
        if (string.IsNullOrEmpty(objectKey))
        {
            return false;
        }

        // 对象键长度不能超过850个字符
        if (objectKey.Length > 850)
        {
            return false;
        }

        // 对象键不能以斜杠开头
        if (objectKey.StartsWith('/'))
        {
            return false;
        }

        // 检查是否包含非法字符
        var invalidChars = new char[] { '\\', '?', '*', ':', '|', '"', '<', '>', '\r', '\n' };
        if (objectKey.IndexOfAny(invalidChars) >= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 标准化对象键
    /// </summary>
    /// <param name="objectKey">原始对象键</param>
    /// <returns>标准化的对象键</returns>
    public static string NormalizeObjectKey(this string objectKey)
    {
        if (string.IsNullOrEmpty(objectKey))
        {
            return objectKey;
        }

        // 移除开头的斜杠
        objectKey = objectKey.TrimStart('/');

        // 替换反斜杠为正斜杠
        objectKey = objectKey.Replace('\\', '/');

        // 移除连续的斜杠
        while (objectKey.Contains("//"))
        {
            objectKey = objectKey.Replace("//", "/");
        }

        return objectKey;
    }

    /// <summary>
    /// 从COS错误中提取错误信息
    /// </summary>
    /// <param name="exception">COS异常</param>
    /// <returns>格式化的错误信息</returns>
    public static string GetFriendlyErrorMessage(this Exception exception)
    {
        return exception switch
        {
            CosServerException serverEx => serverEx.statusCode switch
            {
                403 => "访问被拒绝，请检查密钥和权限配置",
                404 => "资源不存在",
                409 => "资源冲突",
                429 => "请求过于频繁，请稍后重试",
                500 => "服务器内部错误",
                503 => "服务暂时不可用",
                _ => $"服务器错误 ({serverEx.statusCode}): {serverEx.statusMessage}"
            },
            CosClientException clientEx => $"客户端错误: {clientEx.Message}",
            _ => exception.Message
        };
    }
}

/// <summary>
/// 腾讯云COS临时密钥凭证提供程序
/// </summary>
public class TencentCosTemporaryCredentialProvider : DefaultSessionQCloudCredentialProvider
{
    private readonly TencentCosOptions _options;
    private readonly ILogger _logger;
    private DateTime _lastRefreshTime;

    /// <summary>
    /// 初始化临时密钥凭证提供程序
    /// </summary>
    /// <param name="options">COS配置选项</param>
    /// <param name="logger">日志记录器</param>
    public TencentCosTemporaryCredentialProvider(TencentCosOptions options, ILogger logger) 
        : base(null, null, 0L, null)
    {
        _options = options;
        _logger = logger;
        _lastRefreshTime = DateTime.MinValue;
    }

    /// <summary>
    /// 刷新临时密钥
    /// </summary>
    public override void Refresh()
    {
        try
        {
            // 检查是否需要刷新
            var now = DateTime.UtcNow;
            var refreshInterval = TimeSpan.FromSeconds(_options.TemporaryCredentials?.RefreshIntervalSeconds ?? 600);
            
            if (now - _lastRefreshTime < refreshInterval)
            {
                return;
            }

            _logger.LogDebug("开始刷新腾讯云COS临时密钥");

            // TODO: 这里应该调用STS服务获取临时密钥
            // 由于STS服务的具体实现依赖于业务场景，这里提供基本框架
            var temporaryCredentials = GetTemporaryCredentialsFromSts();
            
            if (temporaryCredentials != null)
            {
                SetQCloudCredential(
                    temporaryCredentials.SecretId,
                    temporaryCredentials.SecretKey,
                    $"{temporaryCredentials.StartTime};{temporaryCredentials.ExpiredTime}",
                    temporaryCredentials.Token);

                _lastRefreshTime = now;
                _logger.LogDebug("腾讯云COS临时密钥刷新成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新腾讯云COS临时密钥失败");
            throw;
        }
    }

    /// <summary>
    /// 从STS服务获取临时密钥
    /// </summary>
    /// <returns>临时密钥信息</returns>
    private TemporaryCredentials? GetTemporaryCredentialsFromSts()
    {
        // TODO: 实现STS服务调用
        // 这里需要根据具体的STS服务实现
        _logger.LogWarning("临时密钥获取功能需要具体实现STS服务调用");
        return null;
    }
}

/// <summary>
/// 临时密钥信息
/// </summary>
public class TemporaryCredentials
{
    /// <summary>
    /// 临时SecretId
    /// </summary>
    [DisplayName("临时SecretId")]
    public required string SecretId { get; set; }

    /// <summary>
    /// 临时SecretKey
    /// </summary>
    [DisplayName("临时SecretKey")]
    public required string SecretKey { get; set; }

    /// <summary>
    /// 临时Token
    /// </summary>
    [DisplayName("临时Token")]
    public required string Token { get; set; }

    /// <summary>
    /// 开始时间（Unix时间戳）
    /// </summary>
    [DisplayName("开始时间")]
    public long StartTime { get; set; }

    /// <summary>
    /// 过期时间（Unix时间戳）
    /// </summary>
    [DisplayName("过期时间")]
    public long ExpiredTime { get; set; }
}

/// <summary>
/// 腾讯云COS配置验证器
/// </summary>
public class TencentCosOptionsValidator : IValidateOptions<TencentCosOptions>
{
    /// <summary>
    /// 验证配置
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="options">配置选项</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, TencentCosOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            failures.Add("AppId不能为空");
        }

        if (string.IsNullOrWhiteSpace(options.SecretId))
        {
            failures.Add("SecretId不能为空");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            failures.Add("SecretKey不能为空");
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add("Region不能为空");
        }

        if (options.SignatureDurationSeconds < 60 || options.SignatureDurationSeconds > 7200)
        {
            failures.Add("SignatureDurationSeconds必须在60-7200秒之间");
        }

        if (options.UseTemporaryCredentials)
        {
            if (options.TemporaryCredentials == null)
            {
                failures.Add("启用临时密钥时必须配置TemporaryCredentials");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.TemporaryCredentials.StsEndpoint))
                {
                    failures.Add("临时密钥配置中StsEndpoint不能为空");
                }

                if (options.TemporaryCredentials.DurationSeconds < 900 || options.TemporaryCredentials.DurationSeconds > 7200)
                {
                    failures.Add("临时密钥有效期必须在900-7200秒之间");
                }
            }
        }

        return failures.Count > 0 
            ? ValidateOptionsResult.Fail(failures) 
            : ValidateOptionsResult.Success;
    }
}
