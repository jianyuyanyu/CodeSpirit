using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Messaging.Repositories;
using CodeSpirit.Messaging.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
} 