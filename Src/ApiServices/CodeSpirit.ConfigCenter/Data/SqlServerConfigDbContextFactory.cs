using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeSpirit.Core;
using System.Security.Claims;

namespace CodeSpirit.ConfigCenter.Data;

/// <summary>
/// SQL Server 配置中心数据库上下文工厂
/// 用于设计时工具（如迁移生成）
/// </summary>
public class SqlServerConfigDbContextFactory : IDesignTimeDbContextFactory<SqlServerConfigDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>SQL Server配置中心数据库上下文</returns>
    public SqlServerConfigDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=config-center;Trusted_Connection=True;MultipleActiveResultSets=true;";
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("未找到数据库连接字符串 'DefaultConnection'");
        }

        var optionsBuilder = new DbContextOptionsBuilder<SqlServerConfigDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // 创建服务容器用于依赖注入
        var services = new ServiceCollection();
        
        // 注册必要的服务
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTransient<ICurrentUser, DesignTimeCurrentUser>();
        
        var serviceProvider = services.BuildServiceProvider();

        return new SqlServerConfigDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>());
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
internal class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string UserName => "DesignTime";
    public string? TenantId => "default";
    public string? TenantName => "DefaultTenant";
    public bool IsAuthenticated => false;
    public string[] Roles => Array.Empty<string>();
    public IEnumerable<Claim> Claims => Array.Empty<Claim>();
    public HashSet<string> Permissions => new HashSet<string>();

    /// <summary>
    /// 判断用户是否属于指定角色
    /// </summary>
    /// <param name="role">角色名称</param>
    /// <returns>如果用户属于该角色返回true，否则返回false</returns>
    public bool IsInRole(string role) => false;

    /// <summary>
    /// 判断用户是否属于指定租户
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>如果用户属于该租户返回true，否则返回false</returns>
    public bool IsInTenant(string tenantId) => false;
}
