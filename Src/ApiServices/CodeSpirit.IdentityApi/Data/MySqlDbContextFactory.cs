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
/// 设计时MySqlDbContext工厂，用于EF Core迁移
/// </summary>
public class MySqlDbContextFactory : IDesignTimeDbContextFactory<MySqlDbContext>
{
    public MySqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        
        // MySQL连接字符串 - 使用Aspire映射的端口
        var connectionString = "Server=localhost;Port=3306;Database=identity-api;Uid=root;Pwd=Password123;CharSet=utf8mb4;";
        // 使用固定的服务器版本，避免在设计时需要连接到实际数据库
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql"),
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
        
        return new MySqlDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<EntityFileReferenceEventHandler>()
        );
    }
}
