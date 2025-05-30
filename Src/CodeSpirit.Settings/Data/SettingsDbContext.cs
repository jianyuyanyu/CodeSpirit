using CodeSpirit.Core;
using CodeSpirit.Settings.Models;
using CodeSpirit.Shared.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Settings.Data;

/// <summary>
/// 设置数据库上下文
/// </summary>
public class SettingsDbContext : MultiTenantDbContext
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 设置项
    /// </summary>
    public DbSet<SettingItem> SettingItems { get; set; } = null!;

    /// <summary>
    /// 设置历史
    /// </summary>
    public DbSet<SettingHistory> SettingHistories { get; set; } = null!;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public SettingsDbContext(DbContextOptions<SettingsDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) :
        base(options, serviceProvider, currentUser, httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
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
    /// 配置模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 设置项配置
        modelBuilder.Entity<SettingItem>(entity =>
        {
            // 设置表名
            entity.ToTable("SettingItems");

            // 设置主键
            entity.HasKey(e => e.Id);

            // 基础索引
            entity.HasIndex(e => new { e.Module, e.Key }).IsUnique();
            entity.HasIndex(e => new { e.Module, e.Scope, e.ScopeId });
            
            // 多租户索引
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Id });
            entity.HasIndex(e => new { e.TenantId, e.Module, e.Key }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.Module, e.Scope, e.ScopeId });

            // 将枚举存储为字符串
            entity.Property(e => e.ValueType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(e => e.Scope)
                .HasConversion<string>()
                .HasMaxLength(50);

            // 软删除筛选器
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 设置历史配置
        modelBuilder.Entity<SettingHistory>(entity =>
        {
            // 设置表名
            entity.ToTable("SettingHistories");

            // 设置主键
            entity.HasKey(e => e.Id);

            // 关系
            entity.HasOne(e => e.Setting)
                .WithMany()
                .HasForeignKey(e => e.SettingId)
                .OnDelete(DeleteBehavior.Restrict);

            // 基础索引
            entity.HasIndex(e => e.SettingId);
            entity.HasIndex(e => e.Version);
            
            // 多租户索引
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.Id });
            entity.HasIndex(e => new { e.TenantId, e.SettingId });

            // 软删除筛选器
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}