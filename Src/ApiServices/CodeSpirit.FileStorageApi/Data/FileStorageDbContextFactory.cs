using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.FileStorageApi.Data;

/// <summary>
/// 用于EF Core设计时工具的DbContext工厂
/// </summary>
public class FileStorageDbContextFactory : IDesignTimeDbContextFactory<FileStorageDbContext>
{
    public FileStorageDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FileStorageDbContext>();
        
        // 使用默认的连接字符串进行迁移
        var connectionString = "Server=(LocalDB)\\MSSQLLocalDB;Database=CodeSpirit.FileStorage;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
        optionsBuilder.UseSqlServer(connectionString);

        // 创建一个简单的服务提供程序和当前用户实现用于设计时
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
            
        var httpContextAccessor = new HttpContextAccessor();
        var currentUser = new DesignTimeCurrentUser();

        return new FileStorageDbContext(optionsBuilder.Options, serviceProvider, currentUser, httpContextAccessor);
    }
}

/// <summary>
/// 设计时的当前用户实现
/// </summary>
public class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => 1;
    public string UserId => "design-time-user";
    public string UserName => "Design Time User";
    public string TenantId => "default";
    public string TenantName => "Default Tenant";
    public string[] Roles => new[] { "Admin" };
    public HashSet<string> Permissions => new HashSet<string> { "All" };
    public bool IsAuthenticated => true;
    public string DisplayName => "Design Time User";
    public IEnumerable<System.Security.Claims.Claim> Claims => Array.Empty<System.Security.Claims.Claim>();
    public string? GetClaimValue(string claimType) => null;
    public bool HasRole(string role) => true;
    public bool IsInRole(string role) => true;
    public bool IsInTenant(string tenantId) => true;
    public bool HasPermission(string permission) => true;
}
