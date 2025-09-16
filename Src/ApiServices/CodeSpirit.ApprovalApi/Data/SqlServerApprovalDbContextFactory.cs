using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.ApprovalApi.Data;

/// <summary>
/// SQL Server 审批数据库上下文工厂
/// 用于设计时迁移生成
/// </summary>
public class SqlServerApprovalDbContextFactory : IDesignTimeDbContextFactory<SqlServerApprovalDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">参数</param>
    /// <returns>数据库上下文</returns>
    public SqlServerApprovalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerApprovalDbContext>();
        
        // 使用默认连接字符串
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CodeSpirit.ApprovalApi;Trusted_Connection=true;MultipleActiveResultSets=true");
        
        // 创建模拟的服务提供者和当前用户
        var serviceProvider = new ServiceCollection()
            .AddScoped<ICurrentUser, DesignTimeCurrentUser>()
            .AddScoped<IHttpContextAccessor, DesignTimeHttpContextAccessor>()
            .BuildServiceProvider();
        
        var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        
        return new SqlServerApprovalDbContext(optionsBuilder.Options, serviceProvider, currentUser, httpContextAccessor);
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
public class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => 1;
    public string UserName => "System";
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
/// 设计时HTTP上下文访问器实现
/// </summary>
public class DesignTimeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }
}
