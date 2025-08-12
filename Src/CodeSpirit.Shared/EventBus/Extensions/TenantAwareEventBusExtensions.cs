using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Implementations;
using CodeSpirit.Shared.EventBus.Publishers;

namespace CodeSpirit.Shared.EventBus.Extensions;

/// <summary>
/// 租户感知事件总线扩展方法
/// </summary>
public static class TenantAwareEventBusExtensions
{
    /// <summary>
    /// 添加租户感知事件总线
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTenantAwareEventBus(this IServiceCollection services)
    {
        // 注册租户感知事件总线服务
        services.AddScoped<ITenantAwareEventBus, TenantAwareEventBus>();
        
        // 注册文件引用事件发布器
        services.AddScoped<FileReferenceEventPublisher>();
        
        // 不注册 TenantEventContext 为服务，因为它需要动态创建
        
        return services;
    }

    /// <summary>
    /// 注册租户感知事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTenantAwareEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : ITenantAwareEvent
        where THandler : class, ITenantAwareEventHandler<TEvent>
    {
        // 注册事件处理器
        services.AddTransient<THandler>();
        services.AddTransient<ITenantAwareEventHandler<TEvent>, THandler>();
        services.AddTransient<IEventHandler<TEvent>, THandler>();

        // 注册事件处理器启动时订阅事件
        services.AddSingleton<IHostedService>(sp =>
        {
            return new TenantAwareEventBusSubscriptionHostedService<TEvent, THandler>(
                sp, 
                sp.GetRequiredService<ILogger<TenantAwareEventBusSubscriptionHostedService<TEvent, THandler>>>());
        });

        return services;
    }

    /// <summary>
    /// 租户感知事件总线订阅服务
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    public sealed class TenantAwareEventBusSubscriptionHostedService<TEvent, THandler> : BackgroundService
        where TEvent : ITenantAwareEvent
        where THandler : ITenantAwareEventHandler<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TenantAwareEventBusSubscriptionHostedService<TEvent, THandler>> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="logger">日志记录器</param>
        public TenantAwareEventBusSubscriptionHostedService(
            IServiceProvider serviceProvider,
            ILogger<TenantAwareEventBusSubscriptionHostedService<TEvent, THandler>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 执行订阅
        /// </summary>
        /// <param name="stoppingToken">取消令牌</param>
        /// <returns>执行任务</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("正在启动租户感知事件订阅服务: {Event} -> {Handler}",
                typeof(TEvent).Name, typeof(THandler).Name);

            try
            {
                // 创建作用域来获取租户感知事件总线
                using var scope = _serviceProvider.CreateScope();
                var eventBus = scope.ServiceProvider.GetRequiredService<ITenantAwareEventBus>();
                
                await eventBus.SubscribeAsync<TEvent, THandler>();

                _logger.LogInformation("租户感知事件订阅服务启动完成: {Event} -> {Handler}",
                    typeof(TEvent).Name, typeof(THandler).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "租户感知事件订阅服务启动失败: {Event} -> {Handler}. 这不会导致应用程序停止。",
                    typeof(TEvent).Name, typeof(THandler).Name);
                
                // 不重新抛出异常，防止应用程序崩溃
            }

            // 保持服务运行
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}