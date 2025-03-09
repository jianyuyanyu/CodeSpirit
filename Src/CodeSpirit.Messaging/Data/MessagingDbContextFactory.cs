using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.Messaging.Data;

/// <summary>
/// 消息数据库上下文工厂，用于设计时生成迁移
/// </summary>
public class MessagingDbContextFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    /// <summary>
    /// 创建数据库上下文
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>数据库上下文</returns>
    public MessagingDbContext CreateDbContext(string[] args)
    {
        // 设置配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // 配置选项
        var optionsBuilder = new DbContextOptionsBuilder<MessagingDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("messaging-api") ?? 
            "Server=(localdb)\\mssqllocaldb;Database=codespirit-messaging;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new MessagingDbContext(optionsBuilder.Options);
    }
} 