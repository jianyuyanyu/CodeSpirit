#nullable enable
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// API Key 验证结果 DTO
/// 用于内部验证接口返回，包含用户、租户和权限信息
/// </summary>
[DisplayName("API Key 验证结果")]
public class ApiKeyValidationDto
{
    /// <summary>
    /// API密钥ID
    /// </summary>
    [DisplayName("API密钥ID")]
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    [DisplayName("租户名称")]
    public string? TenantName { get; set; }

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public long UserId { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    [DisplayName("用户信息")]
    public ApiKeyUserDto? User { get; set; }

    /// <summary>
    /// 角色列表（JSON字符串）
    /// </summary>
    [DisplayName("角色")]
    public string? Roles { get; set; }

    /// <summary>
    /// 权限配置（JSON字符串）
    /// </summary>
    [DisplayName("权限")]
    public string? Permissions { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    [DisplayName("最后使用时间")]
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// API Key 用户信息 DTO
/// </summary>
[DisplayName("API Key 用户信息")]
public class ApiKeyUserDto
{
    /// <summary>
    /// 用户ID（字符串类型，用于支持大数字的JSON序列化）
    /// </summary>
    [DisplayName("用户ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    [DisplayName("用户名")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 姓名
    /// </summary>
    [DisplayName("姓名")]
    public string Name { get; set; } = string.Empty;
}

