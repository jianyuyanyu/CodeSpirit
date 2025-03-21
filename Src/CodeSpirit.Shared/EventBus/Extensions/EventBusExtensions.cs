using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.Shared.EventBus.Implementations;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
namespace CodeSpirit.Shared.EventBus.Extensions;

/// <summary>
/// 事件总线扩展方法
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// 添加事件总线
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        // 注册事件总线服务
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        
        // 配置托管服务异常行为，避免因RabbitMQ连接问题导致应用程序崩溃
        services.Configure<HostOptions>(options =>
        {
            // 将BackgroundServiceExceptionBehavior设置为Ignore，避免因事件总线异常导致应用程序停止
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });

        return services;
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <typeparam name="THandler">事件处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
        where THandler : class, IEventHandler<TEvent>
    {
        // 注册事件处理器
        services.AddTransient<THandler>();
        services.AddTransient<IEventHandler<TEvent>, THandler>();

        // 注册事件处理器启动时订阅事件
        services.AddHostedService<EventBusSubscriptionHostedService<TEvent, THandler>>(sp =>
        {
            return new EventBusSubscriptionHostedService<TEvent, THandler>(sp.GetRequiredService<IEventBus>(), sp.GetRequiredService<ILogger<EventBusSubscriptionHostedService<TEvent, THandler>>>());
        });

        return services;
    }

    /// <summary>
    /// 事件总线订阅服务
    /// </summary>
    public sealed class EventBusSubscriptionHostedService<TEvent, THandler> : BackgroundService
        where THandler : IEventHandler<TEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<EventBusSubscriptionHostedService<TEvent, THandler>> _logger;

        public EventBusSubscriptionHostedService(IEventBus eventBus,
            ILogger<EventBusSubscriptionHostedService<TEvent, THandler>> logger)
        {
            _eventBus = eventBus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("正在启动事件订阅服务: {Event} -> {Handler}",
                typeof(TEvent).Name, typeof(THandler).Name);

            try
            {
                await _eventBus.Subscribe<TEvent, THandler>();

                _logger.LogInformation("事件订阅服务启动完成: {Event} -> {Handler}",
                    typeof(TEvent).Name, typeof(THandler).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事件订阅服务启动失败: {Event} -> {Handler}. 这不会导致应用程序停止。",
                    typeof(TEvent).Name, typeof(THandler).Name);
                
                // 不重新抛出异常，防止应用程序崩溃
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // 保持服务运行
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Service} is stopping.",
                nameof(EventBusSubscriptionHostedService<TEvent, THandler>));

            try 
            {
                _eventBus.Unsubscribe<TEvent, THandler>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消订阅事件时发生错误: {Event} -> {Handler}",
                    typeof(TEvent).Name, typeof(THandler).Name);
            }

            return base.StopAsync(cancellationToken);
        }
    }
}