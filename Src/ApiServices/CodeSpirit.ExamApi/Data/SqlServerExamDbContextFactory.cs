using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using CodeSpirit.Core;

namespace CodeSpirit.ExamApi.Data;

/// <summary>
/// SQL Server 考试系统数据库上下文工厂
/// 用于设计时工具（如迁移生成）
/// </summary>
public class SqlServerExamDbContextFactory : IDesignTimeDbContextFactory<SqlServerExamDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>SQL Server考试系统数据库上下文</returns>
    public SqlServerExamDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=(localdb)\\mssqllocaldb;Database=exam-api;Trusted_Connection=True;MultipleActiveResultSets=true;";
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("未找到数据库连接字符串 'DefaultConnection'");
        }

        var optionsBuilder = new DbContextOptionsBuilder<SqlServerExamDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        // 创建服务容器用于依赖注入
        var services = new ServiceCollection();
        
        // 注册必要的服务
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddTransient<ICurrentUser, SqlServerDesignTimeCurrentUser>();
        
        var serviceProvider = services.BuildServiceProvider();

        return new SqlServerExamDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现 - SQL Server
/// </summary>
internal class SqlServerDesignTimeCurrentUser : ICurrentUser
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
