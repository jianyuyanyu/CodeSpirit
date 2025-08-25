using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.SurveyApi.Data;

/// <summary>
/// 问卷数据库上下文工厂，用于设计时操作（如迁移）
/// </summary>
public class SurveyDbContextFactory : IDesignTimeDbContextFactory<SurveyDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">参数</param>
    /// <returns>数据库上下文</returns>
    public SurveyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SurveyDbContext>();
        
        // 使用默认的LocalDB连接字符串
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=codespirit-survey;Trusted_Connection=True;MultipleActiveResultSets=true");

        // 为设计时创建服务容器
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<ICurrentUser, DesignTimeCurrentUser>();
        var serviceProvider = services.BuildServiceProvider();

        return new SurveyDbContext(
            optionsBuilder.Options, 
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
internal class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => null;
    public string UserName => "DesignTimeUser";
    public string[] Roles => Array.Empty<string>();
    public bool IsAuthenticated => false;
    public IEnumerable<System.Security.Claims.Claim> Claims => Enumerable.Empty<System.Security.Claims.Claim>();
    public HashSet<string> Permissions => new HashSet<string>();
    public string? TenantId => "default";
    public string? TenantName => "Default Tenant";
    
    public bool IsInRole(string role) => false;
    public bool IsInTenant(string tenantId) => tenantId == "default";
}
