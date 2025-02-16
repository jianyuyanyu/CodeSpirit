namespace CodeSpirit.ConfigCenter.Data;

using CodeSpirit.ConfigCenter.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// 配置中心数据库上下文
/// </summary>
public class ConfigDbContext : DbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// 应用表
    /// </summary>
    public DbSet<App> Apps { get; set; }

    /// <summary>
    /// 配置项表
    /// </summary>
    public DbSet<ConfigItem> Configs { get; set; }

    /// <summary>
    /// 发布记录表
    /// </summary>
    public DbSet<ConfigPublishHistory> ConfigPublishHistorys { get; set; }

    /// <summary>
    /// 配置实体关系和约束
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
} 