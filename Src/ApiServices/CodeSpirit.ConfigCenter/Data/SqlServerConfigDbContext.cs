using CodeSpirit.Core;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.ConfigCenter.Data;

/// <summary>
/// SQL Server 特定的配置中心数据库上下文
/// 用于迁移和SQL Server特定的配置
/// </summary>
public class SqlServerConfigDbContext : ConfigDbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="currentUser">当前用户</param>
    public SqlServerConfigDbContext(
        DbContextOptions<SqlServerConfigDbContext> options,
        IServiceProvider serviceProvider,
        ICurrentUser currentUser) 
        : base((DbContextOptions)options, serviceProvider, currentUser)
    {
    }

    /// <summary>
    /// 应用数据库特定的配置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void ApplyDatabaseSpecificConfigurations(ModelBuilder modelBuilder)
    {
        // 应用SQL Server特定配置
        DatabaseSpecificConfigurations.ApplySqlServerConfigurations(modelBuilder);
    }
}
