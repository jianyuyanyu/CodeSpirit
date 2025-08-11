using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Options;

/// <summary>
/// 腾讯云COS存储配置选项
/// </summary>
public class TencentCosOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "TencentCos";

    /// <summary>
    /// 应用ID (APPID)
    /// </summary>
    [Required(ErrorMessage = "AppId不能为空")]
    [DisplayName("应用ID")]
    public required string AppId { get; set; }

    /// <summary>
    /// SecretId
    /// </summary>
    [Required(ErrorMessage = "SecretId不能为空")]
    [DisplayName("SecretId")]
    public required string SecretId { get; set; }

    /// <summary>
    /// SecretKey
    /// </summary>
    [Required(ErrorMessage = "SecretKey不能为空")]
    [DisplayName("SecretKey")]
    public required string SecretKey { get; set; }

    /// <summary>
    /// 默认存储桶地域
    /// </summary>
    [Required(ErrorMessage = "Region不能为空")]
    [DisplayName("地域")]
    public required string Region { get; set; }

    /// <summary>
    /// 是否使用HTTPS请求
    /// </summary>
    [DisplayName("启用HTTPS")]
    public bool UseHttps { get; set; } = true;

    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    [DisplayName("启用调试日志")]
    public bool EnableDebugLog { get; set; } = false;

    /// <summary>
    /// 签名有效时长（秒），默认10分钟
    /// </summary>
    [Range(60, 7200, ErrorMessage = "签名有效时长必须在60-7200秒之间")]
    [DisplayName("签名有效时长")]
    public long SignatureDurationSeconds { get; set; } = 600;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    [Range(1000, 60000, ErrorMessage = "连接超时时间必须在1000-60000毫秒之间")]
    [DisplayName("连接超时时间")]
    public int ConnectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 读写超时时间（毫秒）
    /// </summary>
    [Range(5000, 300000, ErrorMessage = "读写超时时间必须在5000-300000毫秒之间")]
    [DisplayName("读写超时时间")]
    public int ReadWriteTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 是否使用临时密钥
    /// </summary>
    [DisplayName("使用临时密钥")]
    public bool UseTemporaryCredentials { get; set; } = false;

    /// <summary>
    /// 临时密钥配置
    /// </summary>
    [DisplayName("临时密钥配置")]
    public TencentCosTemporaryCredentialsOptions? TemporaryCredentials { get; set; }

    /// <summary>
    /// 服务端点域名（可选，用于私有化部署）
    /// </summary>
    [DisplayName("服务端点")]
    public string? ServiceDomain { get; set; }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(AppId) || 
            string.IsNullOrWhiteSpace(SecretId) || 
            string.IsNullOrWhiteSpace(SecretKey) || 
            string.IsNullOrWhiteSpace(Region))
        {
            return false;
        }

        if (UseTemporaryCredentials && TemporaryCredentials == null)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// 腾讯云COS临时密钥配置
/// </summary>
public class TencentCosTemporaryCredentialsOptions
{
    /// <summary>
    /// 临时密钥服务端点URL
    /// </summary>
    [Required(ErrorMessage = "临时密钥服务端点不能为空")]
    [DisplayName("临时密钥服务端点")]
    [Url(ErrorMessage = "临时密钥服务端点格式不正确")]
    public required string StsEndpoint { get; set; }

    /// <summary>
    /// 临时密钥有效期（秒）
    /// </summary>
    [Range(900, 7200, ErrorMessage = "临时密钥有效期必须在900-7200秒之间")]
    [DisplayName("临时密钥有效期")]
    public long DurationSeconds { get; set; } = 1800;

    /// <summary>
    /// 角色ARN（可选）
    /// </summary>
    [DisplayName("角色ARN")]
    public string? RoleArn { get; set; }

    /// <summary>
    /// 角色会话名称（可选）
    /// </summary>
    [DisplayName("角色会话名称")]
    public string? RoleSessionName { get; set; }

    /// <summary>
    /// 权限策略（可选）
    /// </summary>
    [DisplayName("权限策略")]
    public string? Policy { get; set; }

    /// <summary>
    /// 自动刷新间隔（秒），应小于有效期
    /// </summary>
    [Range(300, 3600, ErrorMessage = "自动刷新间隔必须在300-3600秒之间")]
    [DisplayName("自动刷新间隔")]
    public long RefreshIntervalSeconds { get; set; } = 600;
}
