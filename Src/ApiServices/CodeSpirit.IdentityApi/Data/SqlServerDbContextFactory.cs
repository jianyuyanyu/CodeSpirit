using CodeSpirit.Core;
using CodeSpirit.IdentityApi.EventHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.IdentityApi.Data;

/// <summary>
/// 设计时SqlServerDbContext工厂，用于EF Core迁移
/// </summary>
public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<SqlServerDbContext>
{
    public SqlServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqlServerDbContext>();
        
        // SQL Server连接字符串 - 优先从环境变量获取，否则使用默认值
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__identity-api") 
            ?? "Server=localhost,1433;Database=identity-api;User Id=sa;Password=Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true;Connection Timeout=30;";
        
        Console.WriteLine($"SqlServerDbContextFactory 使用连接字符串: {connectionString.Replace("Password123!", "***")}");
        optionsBuilder.UseSqlServer(connectionString,
            options => options.MigrationsAssembly("CodeSpirit.IdentityApi"));
        
        // 创建服务提供者
        var services = new ServiceCollection();
        
        // 添加必要的服务
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<ICurrentUser, DesignTimeCurrentUser>();
        services.AddSingleton<EntityFileReferenceEventHandler>();
        services.AddSingleton<CodeSpirit.Shared.Data.IDataFilter, DesignTimeDataFilter>();
        services.AddLogging();
        
        var serviceProvider = services.BuildServiceProvider();
        
        return new SqlServerDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<EntityFileReferenceEventHandler>()
        );
    }
}
