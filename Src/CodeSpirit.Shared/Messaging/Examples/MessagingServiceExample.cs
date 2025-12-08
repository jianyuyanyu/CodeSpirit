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
    private readonly IChannel _channel;
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
        
        // 创建通道（RabbitMQ.Client 7.x 使用异步方法）
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
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
            // 声明交换机（RabbitMQ.Client 7.x 使用异步方法）
            _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();

            // 声明队列
            _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false).GetAwaiter().GetResult();

            // 绑定队列到交换机
            _channel.QueueBindAsync(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "example.*").GetAwaiter().GetResult();

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
    public async Task SendMessageAsync<T>(T message, string routingKey = "example.message")
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

            // 创建消息属性（RabbitMQ.Client 7.x 直接实例化 BasicProperties）
            var properties = new BasicProperties
            {
                Persistent = true, // 消息持久化
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            // 发布消息（RabbitMQ.Client 7.x 使用异步方法，body 类型改为 ReadOnlyMemory<byte>）
            // 注意：BasicPublishAsync 可能不包含 mandatory 参数，或参数顺序不同
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: new ReadOnlyMemory<byte>(body));

            _logger.LogInformation("消息已发送，路由键: {RoutingKey}, 消息ID: {MessageId}, 类型: {MessageType}",
                routingKey, properties.MessageId, typeof(T).Name);
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
            // 为消费者创建单独的通道（RabbitMQ.Client 7.x 使用异步方法）
            var consumerChannel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // 声明临时队列用于订阅（RabbitMQ.Client 7.x 使用异步方法）
            var tempQueueName = $"{_queueName}.subscriber.{Guid.NewGuid()}";
            consumerChannel.QueueDeclareAsync(
                queue: tempQueueName,
                durable: false,
                exclusive: true,
                autoDelete: true).GetAwaiter().GetResult();

            // 绑定队列到交换机
            consumerChannel.QueueBindAsync(
                queue: tempQueueName,
                exchange: _exchangeName,
                routingKey: routingKey).GetAwaiter().GetResult();

            // 设置QoS（RabbitMQ.Client 7.x 使用异步方法）
            consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false).GetAwaiter().GetResult();

            // 创建消费者（RabbitMQ.Client 7.x 使用 AsyncEventingBasicConsumer）
            var consumer = new AsyncEventingBasicConsumer(consumerChannel);

            // 注册消息接收事件（RabbitMQ.Client 7.x 事件签名可能变化）
            consumer.ReceivedAsync += async (sender, e) =>
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

                    // 确认消息（RabbitMQ.Client 7.x 使用异步方法）
                    await consumerChannel.BasicAckAsync(e.DeliveryTag, false);

                    _logger.LogDebug("消息处理完成，路由键: {RoutingKey}, 消息ID: {MessageId}",
                        e.RoutingKey, e.BasicProperties?.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理消息失败，路由键: {RoutingKey}, 消息ID: {MessageId}",
                        e.RoutingKey, e.BasicProperties?.MessageId);

                    // 拒绝消息并重新入队（RabbitMQ.Client 7.x 使用异步方法）
                    await consumerChannel.BasicNackAsync(e.DeliveryTag, false, true);
                }
            };

            // 开始消费（RabbitMQ.Client 7.x 使用异步方法）
            var consumerTag = consumerChannel.BasicConsumeAsync(
                queue: tempQueueName,
                autoAck: false,
                consumer: consumer).GetAwaiter().GetResult();

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
            var queueInfo = _channel.QueueDeclarePassiveAsync(_queueName).GetAwaiter().GetResult();
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
            var purgedCount = _channel.QueuePurgeAsync(_queueName).GetAwaiter().GetResult();
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