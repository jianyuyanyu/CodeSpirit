using System.ComponentModel;
using CodeSpirit.Amis.Attributes.Columns;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// API密钥列表展示DTO
/// </summary>
public class ApiKeyDto
{
    /// <summary>
    /// API密钥ID
    /// </summary>
    [DisplayName("ID")]
    public long Id { get; set; }

    /// <summary>
    /// 密钥名称
    /// </summary>
    [DisplayName("名称")]
    [TplColumn(template: "${name}")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 密钥描述
    /// </summary>
    [DisplayName("描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 密钥前缀（用于显示，如 sk_****）
    /// </summary>
    [DisplayName("密钥前缀")]
    [TplColumn(template: "${prefix}****")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// 关联用户ID
    /// </summary>
    [DisplayName("用户ID")]
    public long UserId { get; set; }

    /// <summary>
    /// 关联用户名称
    /// </summary>
    [DisplayName("用户")]
    [AggregateField(dataSource: "/api/identity/Users/{value}", template: "{name}")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    [DisplayName("最后使用时间")]
    [DateColumn(FromNow = true)]
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    [DateColumn(FromNow = true)]
    [Badge(VisibleOn = "expiresAt && expiresAt < NOW()", Level = "danger", Text = "已过期")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("状态")]
    [AmisColumn(Type = "switch")]
    [Badge(VisibleOn = "!isActive", Level = "default", Text = "已禁用")]
    public bool IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [DisplayName("创建时间")]
    [DateColumn(FromNow = true)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [DisplayName("租户ID")]
    public string TenantId { get; set; } = string.Empty;
}

