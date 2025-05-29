using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.MultiTenant.Models;

/// <summary>
/// 多租户策略枚举
/// </summary>
public enum TenantStrategy
{
    /// <summary>
    /// 共享数据库，共享表结构，通过TenantId字段区分
    /// </summary>
    [Display(Name = "共享数据库")]
    SharedDatabase = 1,
    
    /// <summary>
    /// 共享数据库，独立表结构（表名前缀区分）
    /// </summary>
    [Display(Name = "独立表结构")]
    SharedDatabaseSeparateSchema = 2,
    
    /// <summary>
    /// 独立数据库
    /// </summary>
    [Display(Name = "独立数据库")]
    SeparateDatabase = 3,
    
    /// <summary>
    /// 混合模式（根据租户配置动态选择）
    /// </summary>
    [Display(Name = "混合模式")]
    Hybrid = 4
} 