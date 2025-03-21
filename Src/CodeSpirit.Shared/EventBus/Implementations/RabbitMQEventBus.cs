using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using CodeSpirit.Shared.EventBus.Interfaces;
using System.Threading;
using System.Net.Sockets;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// RabbitMQ事件总线实现（门面模式）
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQEventPublisher _publisher;
    private readonly RabbitMQEventSubscriber _subscriber;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RabbitMQEventBus(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventPublisher> publisherLogger,
        ILogger<RabbitMQEventSubscriber> subscriberLogger,
        string exchangeName = "codespirit_event_bus",
        int retryCount = 5)
    {
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