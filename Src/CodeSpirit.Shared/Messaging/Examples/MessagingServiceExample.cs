using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CodeSpirit.ServiceDefaults.Messaging;

namespace CodeSpirit.Shared.Messaging.Examples;

/// <summary>
/// 消息服务示例
/// 演示如何使用IRabbitMQServiceFactory创建消息服务
/// </summary>
public class MessagingServiceExample : IDisposable
{
    private readonly IRabbitMQServiceFactory _rabbitMQFactory;
    private readonly ILogger<MessagingServiceExample> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = "codespirit.messaging";
    private readonly string _queueName = "example.queue";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rabbitMQFactory">RabbitMQ服务工厂</param>
    /// <param name="logger">日志记录器</param>
    public MessagingServiceExample(
        IRabbitMQServiceFactory rabbitMQFactory,
        ILogger<MessagingServiceExample> logger)
    {
        _rabbitMQFactory = rabbitMQFactory ?? throw new ArgumentNullException(nameof(rabbitMQFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 获取专用的消息连接
        _connection = _rabbitMQFactory.GetMessagingConnection();
        
        // 创建通道
        _channel = _connection.CreateModel();
        
        // 初始化交换机和队列
        InitializeRabbitMQ();
    }

    /// <summary>
    /// 初始化RabbitMQ交换机和队列
    /// </summary>
    private void InitializeRabbitMQ()
    {
        try
        {
            // 声明交换机
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // 声明队列
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // 绑定队列到交换机
            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "example.*");

            _logger.LogInformation("消息服务RabbitMQ初始化完成，交换机: {ExchangeName}, 队列: {QueueName}",
                _exchangeName, _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息服务RabbitMQ初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="message">消息内容</param>
    /// <param name="routingKey">路由键</param>
    /// <returns>发送任务</returns>
    public Task SendMessageAsync<T>(T message, string routingKey = "example.message")
    {
        if (_channel == null || !_channel.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ通道未打开");
        }

        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var body = Encoding.UTF8.GetBytes(json);

            // 创建消息属性
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 消息持久化
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";

            // 发布消息
            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("消息已发送，路由键: {RoutingKey}, 消息ID: {MessageId}, 类型: {MessageType}",
                routingKey, properties.MessageId, typeof(T).Name);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败，路由键: {RoutingKey}", routingKey);
            throw;
        }
    }

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <param name="routingKey">路由键模式</param>
    /// <returns>消费者标识</returns>
    public string SubscribeMessage<T>(Func<T, Task> handler, string routingKey = "example.*")
    {
        if (_connection == null || !_connection.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ连接未打开");
        }

        try
        {
            // 为消费者创建单独的通道
            var consumerChannel = _connection.CreateModel();

            // 声明临时队列用于订阅
            var tempQueueName = $"{_queueName}.subscriber.{Guid.NewGuid()}";
            consumerChannel.QueueDeclare(
                queue: tempQueueName,
                durable: false,
                exclusive: true,
                autoDelete: true);

            // 绑定队列到交换机
            consumerChannel.QueueBind(
                queue: tempQueueName,
                exchange: _exchangeName,
                routingKey: routingKey);

            // 设置QoS
            consumerChannel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

            // 创建消费者
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);

            // 注册消息接收事件
            consumer.Received += async (sender, e) =>
            {
                var body = e.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogDebug("收到消息，路由键: {RoutingKey}, 消息ID: {MessageId}",
                        e.RoutingKey, e.BasicProperties?.MessageId);

                    await handler(message);

                    // 确认消息
                    consumerChannel.BasicAck(e.DeliveryTag, false);

                    _logger.LogDebug("消息处理完成，路由键: {RoutingKey}, 消息ID: {MessageId}",
                        e.RoutingKey, e.BasicProperties?.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理消息失败，路由键: {RoutingKey}, 消息ID: {MessageId}",
                        e.RoutingKey, e.BasicProperties?.MessageId);

                    // 拒绝消息并重新入队
                    consumerChannel.BasicNack(e.DeliveryTag, false, true);
                }
            };

            // 开始消费
            var consumerTag = consumerChannel.BasicConsume(
                queue: tempQueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("消息订阅已创建，队列: {QueueName}, 消费者标签: {ConsumerTag}, 路由键: {RoutingKey}",
                tempQueueName, consumerTag, routingKey);

            return consumerTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建消息订阅失败，路由键: {RoutingKey}", routingKey);
            throw;
        }
    }

    /// <summary>
    /// 获取队列消息数量
    /// </summary>
    /// <returns>消息数量</returns>
    public uint GetQueueMessageCount()
    {
        try
        {
            var queueInfo = _channel.QueueDeclarePassive(_queueName);
            return queueInfo.MessageCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取队列消息数量失败");
            return 0;
        }
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    /// <returns>清空的消息数量</returns>
    public uint PurgeQueue()
    {
        try
        {
            var purgedCount = _channel.QueuePurge(_queueName);
            _logger.LogInformation("队列已清空，清除消息数量: {Count}", purgedCount);
            return purgedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空队列失败");
            throw;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _logger.LogInformation("消息服务示例已释放资源");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放消息服务示例资源失败");
        }
    }
}

/// <summary>
/// 示例消息类型
/// </summary>
public record ExampleMessage(
    string Id,
    string Content,
    DateTime Timestamp,
    string? Category = null);

/// <summary>
/// 示例通知消息类型
/// </summary>
public record NotificationMessage(
    string UserId,
    string Title,
    string Message,
    DateTime CreatedAt,
    string Type = "Info"); 