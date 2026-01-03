using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Models;
using CodeSpirit.Shared.Entities.Interfaces;

namespace CodeSpirit.IdentityApi.Data.Models;

/// <summary>
/// 第三方账号关联实体
/// </summary>
public class ThirdPartyAccount : IMultiTenant, IFullAuditable, ISoftDeleteAuditable
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的用户ID
    /// </summary>
    [Required]
    public long UserId { get; set; }
    
    /// <summary>
    /// 平台类型
    /// </summary>
    [Required]
    public ThirdPartyPlatformType PlatformType { get; set; }
    
    /// <summary>
    /// 平台OpenId（平台内唯一标识）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OpenId { get; set; } = string.Empty;
    
    /// <summary>
    /// 平台UnionId（跨平台统一标识，可选）
    /// </summary>
    [MaxLength(100)]
    public string? UnionId { get; set; }
    
    /// <summary>
    /// 会话密钥（用于后续API调用，加密存储）
    /// </summary>
    [MaxLength(200)]
    public string? SessionKey { get; set; }
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }
    
    /// <summary>
    /// 是否为主账号（一个用户可以有多个第三方账号，但只有一个主账号）
    /// </summary>
    public bool IsPrimary { get; set; }
    
    // 导航属性
    /// <summary>
    /// 关联的用户
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }
    
    // 审计字段
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // 软删除字段
    public long? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}

