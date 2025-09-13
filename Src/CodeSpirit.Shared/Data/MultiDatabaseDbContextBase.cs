using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Shared.Data;

/// <summary>
/// 多数据库支持的DbContext基类
/// 继承自MultiTenantDbContext，支持MySQL和SQL Server
/// </summary>
public abstract class MultiDatabaseDbContextBase : MultiTenantDbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    protected MultiDatabaseDbContextBase(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor) 
        : base(options, serviceProvider, currentUser, httpContextAccessor)
    {
    }

    /// <summary>
    /// 配置模型创建
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 调用基类的模型创建
        base.OnModelCreating(modelBuilder);
        
        // 应用数据库特定配置
        ApplyDatabaseSpecificConfigurations(modelBuilder);
    }

    /// <summary>
    /// 应用数据库特定的配置
    /// 子类可以重写此方法以应用自定义配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ApplyDatabaseSpecificConfigurations(ModelBuilder modelBuilder)
    {
        // 默认不应用任何特定配置
        // 子类（如MySqlDbContext、SqlServerDbContext）会重写此方法
    }
}

/// <summary>
/// 多数据库支持的AuditableDbContext基类
/// 继承自AuditableDbContext，支持MySQL和SQL Server
/// </summary>
public abstract class MultiDatabaseAuditableDbContextBase : AuditableDbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    protected MultiDatabaseAuditableDbContextBase(
        DbContextOptions options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser) 
        : base(options, serviceProvider, currentUser)
    {
    }

    /// <summary>
    /// 配置模型创建
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 调用基类的模型创建
        base.OnModelCreating(modelBuilder);
        
        // 应用数据库特定配置
        ApplyDatabaseSpecificConfigurations(modelBuilder);
    }

    /// <summary>
    /// 应用数据库特定的配置
    /// 子类可以重写此方法以应用自定义配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected virtual void ApplyDatabaseSpecificConfigurations(ModelBuilder modelBuilder)
    {
        // 默认不应用任何特定配置
        // 子类（如MySqlDbContext、SqlServerDbContext）会重写此方法
    }
}
