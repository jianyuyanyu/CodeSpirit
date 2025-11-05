using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeSpirit.PathfinderApi.Data;

/// <summary>
/// MySQL DbContext 设计时工厂（用于 EF Core 迁移）
/// </summary>
public class MySqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MySqlPathfinderDbContext>
{
    public MySqlPathfinderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MySqlPathfinderDbContext>();
        
        // 使用默认连接字符串（仅用于迁移生成，不需要实际连接）
        optionsBuilder.UseMySql(
            "Server=localhost;Database=pathfinder;",
            new MySqlServerVersion(new Version(8, 0, 21))
        );

        // 创建一个临时的 ServiceProvider 用于迁移
        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddScoped<ICurrentUser>(sp => new DesignTimeCurrentUser())
            .AddHttpContextAccessor()
            .BuildServiceProvider();

        return new MySqlPathfinderDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()
        );
    }
}

/// <summary>
/// SQL Server DbContext 设计时工厂（用于 EF Core 迁移）
/// </summary>
public class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerPathfinderDbContext>
{
    public SqlServerPathfinderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerPathfinderDbContext>();
        
        // 使用默认连接字符串（仅用于迁移生成）
        optionsBuilder.UseSqlServer("Server=localhost;Database=pathfinder;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;");

        // 创建一个临时的 ServiceProvider 用于迁移
        var serviceProvider = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddScoped<ICurrentUser>(sp => new DesignTimeCurrentUser())
            .AddHttpContextAccessor()
            .BuildServiceProvider();

        return new SqlServerPathfinderDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()
        );
    }
}

/// <summary>
/// 设计时的 CurrentUser 实现（用于迁移）
/// </summary>
internal class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string UserName => "system";
    public string[] Roles => Array.Empty<string>();
    public bool IsAuthenticated => false;
    public IEnumerable<System.Security.Claims.Claim> Claims => Enumerable.Empty<System.Security.Claims.Claim>();
    public HashSet<string> Permissions => new HashSet<string>();
    public string? TenantId => null;
    public string? TenantName => null;
    
    public bool IsInRole(string role) => false;
    public bool IsInTenant(string tenantId) => false;
}

