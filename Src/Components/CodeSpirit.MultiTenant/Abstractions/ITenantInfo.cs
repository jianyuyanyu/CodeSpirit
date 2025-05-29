using CodeSpirit.MultiTenant.Models;

namespace CodeSpirit.MultiTenant.Abstractions;

/// <summary>
/// 租户信息接口
/// </summary>
public interface ITenantInfo
{
    /// <summary>
    /// 租户ID
    /// </summary>
    string TenantId { get; }
    
    /// <summary>
    /// 租户名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 租户显示名称
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// 租户描述
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 租户策略
    /// </summary>
    TenantStrategy Strategy { get; }
    
    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    string ConnectionString { get; }
    
    /// <summary>
    /// 表前缀（用于SharedDatabaseSeparateSchema策略）
    /// </summary>
    string TablePrefix { get; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// 租户配置（JSON格式）
    /// </summary>
    string Configuration { get; }
    
    /// <summary>
    /// 租户域名
    /// </summary>
    string Domain { get; }
    
    /// <summary>
    /// 租户Logo URL
    /// </summary>
    string LogoUrl { get; }
    
    /// <summary>
    /// 租户主题配置
    /// </summary>
    string ThemeConfig { get; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    DateTime? UpdatedAt { get; }
} 