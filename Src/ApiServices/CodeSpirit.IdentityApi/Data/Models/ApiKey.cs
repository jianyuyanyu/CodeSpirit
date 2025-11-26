using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Core;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Data.Models;

/// <summary>
/// API密钥实体
/// 用于服务间调用的认证
/// </summary>
public class ApiKey : IMultiTenant, IFullAuditable, ISoftDeleteAuditable, IIsActive
{
    /// <summary>
    /// API密钥唯一标识
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; }

    /// <summary>
    /// API密钥名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// API密钥描述（便于用户标记用途）
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 密钥前缀（如 sk_xxx，便于识别）
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Prefix { get; set; }

    /// <summary>
    /// 密钥的SHA256哈希值
    /// 不存储明文密钥，仅存储哈希值
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string KeyHash { get; set; }

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [Required]
    public long UserId { get; set; }

    /// <summary>
    /// 导航属性，指向用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// 过期时间（可选）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 最后使用时间（便于监控使用情况）
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 权限配置（JSON字符串，预留权限配置）
    /// 格式示例: ["read:exam", "write:exam"]
    /// </summary>
    [MaxLength(2000)]
    public string? Permissions { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public long CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 删除人（软删除）
    /// </summary>
    public long? DeletedBy { get; set; }

    /// <summary>
    /// 删除时间（软删除）
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 是否已删除（软删除标记）
    /// </summary>
    public bool IsDeleted { get; set; }
}

