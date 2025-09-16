using CodeSpirit.Core;
using CodeSpirit.IdentityApi.EventHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;

namespace CodeSpirit.IdentityApi.Data;

/// <summary>
/// 设计时ApplicationDbContext工厂，用于EF Core迁移
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // 获取数据库类型
        var databaseType = Environment.GetEnvironmentVariable("DatabaseType") ?? "MySql";
        
        if (databaseType.Equals("MySql", StringComparison.OrdinalIgnoreCase))
        {
            // MySQL连接字符串 - 使用Aspire映射的端口
            var connectionString = "Server=localhost;Port=3306;Database=identity-api;Uid=root;Pwd=Password123;CharSet=utf8mb4;";
            // 使用固定的服务器版本，避免在设计时需要连接到实际数据库
            optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql"),
                options => options.MigrationsAssembly("CodeSpirit.IdentityApi"));
        }
        else
        {
            // SQL Server连接字符串
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=identity-api;Trusted_Connection=True;MultipleActiveResultSets=true;";
            optionsBuilder.UseSqlServer(connectionString,
                options => options.MigrationsAssembly("CodeSpirit.IdentityApi"));
        }
        
        // 创建服务提供者
        var services = new ServiceCollection();
        
        // 添加必要的服务
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<ICurrentUser, DesignTimeCurrentUser>();
        services.AddSingleton<EntityFileReferenceEventHandler>();
        services.AddSingleton<CodeSpirit.Shared.Data.IDataFilter, DesignTimeDataFilter>();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        
        return new ApplicationDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<EntityFileReferenceEventHandler>()
        );
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
public class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => 1;
    public string UserName => "system";
    public string[] Roles => new[] { "Admin" };
    public bool IsAuthenticated => true;
    public IEnumerable<System.Security.Claims.Claim> Claims => Array.Empty<System.Security.Claims.Claim>();
    public HashSet<string> Permissions => new HashSet<string>();
    public string? TenantId => "default";
    public string? TenantName => "Default Tenant";
    
    public bool IsInRole(string role) => Roles.Contains(role);
    public bool IsInTenant(string tenantId) => TenantId == tenantId;
}

/// <summary>
/// 设计时数据过滤器实现
/// </summary>
public class DesignTimeDataFilter : CodeSpirit.Shared.Data.IDataFilter
{
    public IDisposable Enable<TFilter>() where TFilter : class
    {
        return new DisposableAction(() => { });
    }

    public IDisposable Disable<TFilter>() where TFilter : class
    {
        return new DisposableAction(() => { });
    }

    public bool IsEnabled<TFilter>() where TFilter : class
    {
        return false;
    }
}

/// <summary>
/// 简单的IDisposable实现
/// </summary>
public class DisposableAction : IDisposable
{
    private readonly Action _action;

    public DisposableAction(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        _action?.Invoke();
    }
}
