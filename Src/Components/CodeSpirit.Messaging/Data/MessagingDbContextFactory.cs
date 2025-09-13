using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql;

namespace CodeSpirit.Messaging.Data;

/// <summary>
/// 消息数据库上下文工厂，用于设计时生成迁移
/// </summary>
public class MessagingDbContextFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>数据库上下文</returns>
    public MessagingDbContext CreateDbContext(string[] args)
    {
        // 设置配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // 配置选项 - 根据配置选择数据库类型
        var optionsBuilder = new DbContextOptionsBuilder();
        var connectionString = configuration.GetConnectionString("messaging-api") ?? 
            "Server=localhost;Port=3306;Database=codespirit-messaging;Uid=root;Pwd=Password123;CharSet=utf8mb4;";
        var databaseType = configuration.GetValue<string>("DatabaseType") ?? "MySql";
        
        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql"));
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        // 创建服务集合用于设计时
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<ICurrentUser, DesignTimeCurrentUser>();
        var serviceProvider = services.BuildServiceProvider();

        return new MessagingDbContext(
            optionsBuilder.Options, 
            serviceProvider, 
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
public class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string UserName => "DesignTime";
    public string[] Roles => Array.Empty<string>();
    public bool IsAuthenticated => false;
    public IEnumerable<System.Security.Claims.Claim> Claims => Enumerable.Empty<System.Security.Claims.Claim>();
    public HashSet<string> Permissions => new HashSet<string>();
    public string? TenantId => "default";
    public string? TenantName => "Default Tenant";

    public bool IsInRole(string role) => false;
    public bool IsInTenant(string tenantId) => tenantId == "default";
} 