namespace CodeSpirit.ConfigCenter.Data;

using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.Core;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;
using System;

/// <summary>
/// 配置中心数据库上下文
/// </summary>
public class ConfigDbContext : AuditableDbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options, IServiceProvider serviceProvider, ICurrentUser currentUser) : base(options, serviceProvider, currentUser)
    {
    }

    /// <summary>
    /// 用于审计的用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 获取当前用户ID，优先使用设置的UserId，否则使用CurrentUser中的Id
    /// </summary>
    protected override long? CurrentUserId => this.UserId ?? base.CurrentUserId;

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
    /// 配置项发布历史表
    /// </summary>
    public DbSet<ConfigItemPublishHistory> ConfigItemPublishHistorys { get; set; }

    /// <summary>
    /// 配置实体关系和约束
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<App>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(36);
        });
        
        // 配置ConfigItemPublishHistory关系，避免循环级联
        modelBuilder.Entity<ConfigItemPublishHistory>(entity =>
        {
            // 配置与ConfigItem的关系为限制级联删除
            entity.HasOne(e => e.ConfigItem)
                .WithMany()
                .HasForeignKey(e => e.ConfigItemId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置与ConfigPublishHistory的关系
            entity.HasOne(e => e.ConfigPublishHistory)
                .WithMany(e => e.ConfigItemPublishHistories)
                .HasForeignKey(e => e.ConfigPublishHistoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // 配置ConfigPublishHistory与App的关系
        modelBuilder.Entity<ConfigPublishHistory>(entity =>
        {
            entity.HasOne(e => e.App)
                .WithMany()
                .HasForeignKey(e => e.AppId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        base.OnModelCreating(modelBuilder);
    }
}