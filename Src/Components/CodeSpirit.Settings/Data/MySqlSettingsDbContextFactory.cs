using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using CodeSpirit.Core;

namespace CodeSpirit.Settings.Data;

/// <summary>
/// MySQL 设置系统数据库上下文工厂
/// 用于设计时工具（如迁移生成）
/// </summary>
public class MySqlSettingsDbContextFactory : IDesignTimeDbContextFactory<MySqlSettingsDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>MySQL设置系统数据库上下文</returns>
    public MySqlSettingsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Port=53362;Database=settings;Uid=root;Pwd=Password123;CharSet=utf8mb4;";
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("未找到数据库连接字符串 'DefaultConnection'");
        }

        var optionsBuilder = new DbContextOptionsBuilder<MySqlSettingsDbContext>();
        // 使用固定的服务器版本，避免在设计时需要连接到实际数据库
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql"));

        // 创建服务容器用于依赖注入
        var services = new ServiceCollection();
        
        // 注册必要的服务
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddTransient<ICurrentUser, MySqlDesignTimeCurrentUser>();
        
        var serviceProvider = services.BuildServiceProvider();

        return new MySqlSettingsDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现 - MySQL
/// </summary>
internal class MySqlDesignTimeCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string UserName => "DesignTime";
    public string? TenantId => "default";
    public string? TenantName => "default";
    public bool IsAuthenticated => false;
    public string[] Roles => Array.Empty<string>();
    public HashSet<string> Permissions => new();
    public IEnumerable<System.Security.Claims.Claim> Claims => Array.Empty<System.Security.Claims.Claim>();
    
    public bool IsInRole(string role) => false;
    public bool IsInTenant(string tenantId) => tenantId == "default";
}
