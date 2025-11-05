using CodeSpirit.Core;
using CodeSpirit.PathfinderApi.Models;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.PathfinderApi.Data;

/// <summary>
/// Pathfinder数据库上下文 - 支持多租户和多数据库
/// </summary>
public class PathfinderDbContext : MultiDatabaseDbContextBase
{
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public PathfinderDbContext(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) : base(options, serviceProvider, currentUser, httpContextAccessor)
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

    #region DbSet Properties
    
    /// <summary>
    /// 目标
    /// </summary>
    public DbSet<Goal> Goals => Set<Goal>();

    /// <summary>
    /// 任务
    /// </summary>
    public DbSet<PathfinderTask> Tasks => Set<PathfinderTask>();
    
    #endregion

    /// <summary>
    /// 配置实体模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Goal实体配置
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.TenantId, e.UserId });
            entity.HasIndex(e => e.CreatedAt);
            
            // 导航属性配置
            entity.HasMany(e => e.Tasks)
                  .WithOne(e => e.Goal)
                  .HasForeignKey(e => e.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Task实体配置
        modelBuilder.Entity<PathfinderTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.GoalId);
            entity.HasIndex(e => e.ParentTaskId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.GoalId, e.Order });
            
            // 自引用关系（父子任务）
            entity.HasOne(e => e.ParentTask)
                  .WithMany(e => e.SubTasks)
                  .HasForeignKey(e => e.ParentTaskId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

