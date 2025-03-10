using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Data.Seeders;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Messaging.Repositories;
using CodeSpirit.Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Messaging.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加消息模块服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册数据库上下文
        services.AddDbContext<MessagingDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("messaging-api"),
                sqlOptions => sqlOptions.EnableRetryOnFailure());
        });

        // 注册仓储
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        // 注册服务
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatService, ChatService>();

        // 注册数据种子服务
        services.AddScoped<MessageSeeder>();
        services.AddScoped<ConversationSeeder>();

        // 添加控制器
        services.AddControllers();

        return services;
    }

    /// <summary>
    /// 添加实时聊天服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRealtimeChat(this IServiceCollection services)
    {
        // 添加SignalR服务
        services.AddSignalR();

        return services;
    }

    /// <summary>
    /// 迁移数据库并初始化数据
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public static async Task MigrateAndSeedMessagingDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<MessagingDbContext>>();
        
        try
        {
            // 执行迁移
            var dbContext = services.GetRequiredService<MessagingDbContext>();
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("消息数据库迁移完成");
            
            // 初始化种子数据
            var messageSeeder = services.GetRequiredService<MessageSeeder>();
            await messageSeeder.SeedSystemNotificationsAsync();
            
            var conversationSeeder = services.GetRequiredService<ConversationSeeder>();
            await conversationSeeder.SeedSampleConversationsAsync();
            
            logger.LogInformation("消息数据初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "消息数据库迁移或初始化失败: {Message}", ex.Message);
            throw;
        }
    }
} 