using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// 创建API密钥响应DTO
/// 包含明文密钥，仅在创建时返回一次
/// </summary>
public class CreateApiKeyResultDto
{
    /// <summary>
    /// API密钥ID
    /// </summary>
    [DisplayName("密钥ID")]
    public long Id { get; set; }

    /// <summary>
    /// 密钥名称
    /// </summary>
    [DisplayName("密钥名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 密钥描述
    /// </summary>
    [DisplayName("密钥描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 完整的API密钥（明文）
    /// 格式：sk_{32个随机字符}
    /// ⚠️ 仅在创建时返回一次，请妥善保存
    /// </summary>
    [DisplayName("API密钥")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥前缀
    /// </summary>
    [DisplayName("密钥前缀")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}

