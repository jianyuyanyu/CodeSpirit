using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using CodeSpirit.Shared.EventBus.Interfaces;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// RabbitMQ事件发布者实现
/// </summary>
public class RabbitMQEventPublisher : RabbitMQEventBusBase, IEventPublisher
{
    private readonly ILogger<RabbitMQEventPublisher> _publisherLogger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RabbitMQEventPublisher(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventPublisher> logger,
        string exchangeName = "codespirit_event_bus",
        int retryCount = 5)
        : base(connection, serviceProvider, logger, exchangeName, retryCount)
    {
        _publisherLogger = logger;
        _channel = CreateChannel();
    }

    /// <summary>
    /// 连接丢失时的处理
    /// </summary>
    protected override void OnConnectionLost()
    {
        TryConnectWithRetry(CreateChannel);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        if (@event == null)
        {
            _publisherLogger.LogWarning("尝试发布null事件");
            throw new ArgumentNullException(nameof(@event));
        }

        if (_disposed)
        {
            _publisherLogger.LogError("事件发布者已被释放，无法发布事件");
            throw new ObjectDisposedException(nameof(RabbitMQEventPublisher));
        }

        var eventName = @event.GetType().Name;
        var message = JsonConvert.SerializeObject(@event, _jsonSettings);
        var body = Encoding.UTF8.GetBytes(message);

        await PublishMessageWithRetryAsync(eventName, body);
    }

    /// <summary>
    /// 使用重试机制发布消息
    /// </summary>
    private async Task PublishMessageWithRetryAsync(string eventName, byte[] body)
    {
        int attempt = 0;
        bool published = false;

        while (!published && attempt < _retryCount)
        {
            try
            {
                // 确保有可用的通道
                await EnsureChannelAsync(CreateChannel);

                var properties = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent, // 持久化消息（RabbitMQ.Client 7.x 使用枚举）
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await _channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: new ReadOnlyMemory<byte>(body));

                _publisherLogger.LogInformation("已发布事件: {EventName}，MessageId: {MessageId}", 
                    eventName, properties.MessageId);
                published = true;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException || ex is SocketException || ex is AlreadyClosedException)
            {
                attempt++;
                if (attempt >= _retryCount)
                {
                    _publisherLogger.LogError(ex, "发布事件失败（尝试 {Attempt}/{RetryCount}）: {EventName}",
                        attempt, _retryCount, eventName);
                    throw;
                }

                var waitTime = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _publisherLogger.LogWarning(ex, "发布事件失败（尝试 {Attempt}/{RetryCount}），{TimeSpan}秒后重试...",
                    attempt, _retryCount, waitTime.TotalSeconds);

                await Task.Delay(waitTime);
            }
            catch (Exception ex)
            {
                _publisherLogger.LogError(ex, "发布事件失败: {EventName}", eventName);
                throw;
            }
        }
    }
} 