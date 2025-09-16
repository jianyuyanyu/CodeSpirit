using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeSpirit.Core;

namespace CodeSpirit.ConfigCenter.Data;

/// <summary>
/// MySQL 配置中心数据库上下文工厂
/// 用于设计时工具（如迁移生成）
/// </summary>
public class MySqlConfigDbContextFactory : IDesignTimeDbContextFactory<MySqlConfigDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>MySQL配置中心数据库上下文</returns>
    public MySqlConfigDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Port=3306;Database=config-center;Uid=root;Pwd=Password123;CharSet=utf8mb4;";
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("未找到数据库连接字符串 'DefaultConnection'");
        }

        var optionsBuilder = new DbContextOptionsBuilder<MySqlConfigDbContext>();
        // 使用固定的服务器版本，避免在设计时需要连接到实际数据库
        optionsBuilder.UseMySql(connectionString, ServerVersion.Parse("8.0.21-mysql"));

        // 创建服务容器用于依赖注入
        var services = new ServiceCollection();
        
        // 注册必要的服务
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTransient<ICurrentUser, DesignTimeCurrentUser>();
        
        var serviceProvider = services.BuildServiceProvider();

        return new MySqlConfigDbContext(
            optionsBuilder.Options,
            serviceProvider,
            serviceProvider.GetRequiredService<ICurrentUser>());
    }
}

