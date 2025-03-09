using CodeSpirit.Messaging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.Messaging;

/// <summary>
/// 数据库迁移应用工具类
/// </summary>
public static class ApplyMigrations
{
    /// <summary>
    /// 应用数据库迁移
    /// </summary>
    public static void Apply()
    {
        Console.WriteLine("开始应用数据库迁移...");
        
        try
        {
            // 创建配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // 获取连接字符串
            var connectionString = configuration.GetConnectionString("messaging-api") ?? 
                "Server=(localdb)\\mssqllocaldb;Database=codespirit-messaging;Trusted_Connection=True;MultipleActiveResultSets=true";
            
            Console.WriteLine($"使用连接字符串: {connectionString}");

            // 创建DbContext选项
            var optionsBuilder = new DbContextOptionsBuilder<MessagingDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // 创建DbContext
            using var context = new MessagingDbContext(optionsBuilder.Options);
            
            // 应用迁移
            Console.WriteLine("正在应用迁移...");
            context.Database.Migrate();
            
            Console.WriteLine("数据库迁移应用成功！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用迁移时发生错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
} 