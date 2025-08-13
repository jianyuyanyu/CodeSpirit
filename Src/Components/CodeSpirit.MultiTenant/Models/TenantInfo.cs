using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Entities.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.MultiTenant.Models;

/// <summary>
/// 租户信息实体
/// </summary>
public class TenantInfo : EntityBase<string>, ITenantInfo, ISoftDeleteAuditable, IFullEntityEvent
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户显示名称
    /// </summary>
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户策略
    /// </summary>
    [Required]
    public TenantStrategy Strategy { get; set; } = TenantStrategy.SharedDatabase;
    
    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    [StringLength(1000)]
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 表前缀
    /// </summary>
    [StringLength(20)]
    public string TablePrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 租户配置（JSON格式）
    /// </summary>
    public string Configuration { get; set; } = "{}";
    
    /// <summary>
    /// 租户域名
    /// </summary>
    [StringLength(100)]
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户Logo URL
    /// </summary>
    [StringLength(500)]
    public string LogoUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户主题配置
    /// </summary>
    public string ThemeConfig { get; set; } = "{}";
    
    /// <summary>
    /// 最大用户数限制
    /// </summary>
    public int MaxUsers { get; set; } = 1000;
    
    /// <summary>
    /// 存储空间限制（MB）
    /// </summary>
    public long StorageLimit { get; set; } = 10240; // 10GB
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 创建人
    /// </summary>
    public long CreatedBy { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 更新人
    /// </summary>
    public long? UpdatedBy { get; set; }
    
    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// 删除人
    /// </summary>
    public long? DeletedBy { get; set; }
} 