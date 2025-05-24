using CodeSpirit.Shared.EventBus.Interfaces;
using CodeSpirit.ServiceDefaults.Messaging;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// RabbitMQ事件总线实现（门面模式）
/// 基于Aspire.RabbitMQ.Client集成重构
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQEventPublisher _publisher;
    private readonly RabbitMQEventSubscriber _subscriber;
    private bool _disposed;

    /// <summary>
    /// 构造函数 - 使用RabbitMQ服务工厂
    /// </summary>
    /// <param name="rabbitMQServiceFactory">RabbitMQ服务工厂</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="publisherLogger">发布者日志记录器</param>
    /// <param name="subscriberLogger">订阅者日志记录器</param>
    /// <param name="exchangeName">交换机名称</param>
    /// <param name="retryCount">重试次数</param>
    public RabbitMQEventBus(
        IRabbitMQServiceFactory rabbitMQServiceFactory,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventPublisher> publisherLogger,
        ILogger<RabbitMQEventSubscriber> subscriberLogger,
        string exchangeName = "codespirit_event_bus",
        int retryCount = 5)
    {
        if (rabbitMQServiceFactory == null)
            throw new ArgumentNullException(nameof(rabbitMQServiceFactory));

        // 获取事件总线专用连接
        var connection = rabbitMQServiceFactory.GetEventBusConnection();

        _publisher = new RabbitMQEventPublisher(
            connection,
            serviceProvider,
            publisherLogger,
            exchangeName,
            retryCount);

        _subscriber = new RabbitMQEventSubscriber(
            connection,
            serviceProvider,
            subscriberLogger,
            exchangeName,
            retryCount);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public Task PublishAsync<TEvent>(TEvent @event)
    {
        return _publisher.PublishAsync(@event);
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public async Task Subscribe<TEvent, THandler>()
        where THandler : IEventHandler<TEvent>
    {
        await _subscriber.Subscribe<TEvent, THandler>();
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    public void Unsubscribe<TEvent, THandler>()
        where THandler : IEventHandler<TEvent>
    {
        _subscriber.Unsubscribe<TEvent, THandler>();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _publisher.Dispose();
        _subscriber.Dispose();
    }
}