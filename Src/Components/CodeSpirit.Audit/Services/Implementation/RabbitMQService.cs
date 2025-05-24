using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CodeSpirit.Audit.Models;
using CodeSpirit.ServiceDefaults.Messaging;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// RabbitMQ服务实现
/// 基于Aspire.RabbitMQ.Client集成重构
/// </summary>
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQ.Client.IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly RabbitMQOptions _options;
    private readonly Dictionary<string, IModel> _consumerChannels = new Dictionary<string, IModel>();
    
    /// <summary>
    /// 构造函数 - 使用RabbitMQ服务工厂
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="configuration">配置</param>
    /// <param name="rabbitMQServiceFactory">RabbitMQ服务工厂</param>
    public RabbitMQService(
        ILogger<RabbitMQService> logger, 
        IConfiguration configuration, 
        IRabbitMQServiceFactory rabbitMQServiceFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (rabbitMQServiceFactory == null)
            throw new ArgumentNullException(nameof(rabbitMQServiceFactory));

        // 获取审计专用连接
        _connection = rabbitMQServiceFactory.GetAuditConnection();
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options.RabbitMQ;
        
        try
        {
            // 使用注入的连接创建通道
            _channel = _connection.CreateModel();
            
            // 声明交换机
            _channel.ExchangeDeclare(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            
            // 声明队列
            _channel.QueueDeclare(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            // 绑定队列到交换机
            _channel.QueueBind(
                queue: _options.QueueName,
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey);
            
            _logger.LogInformation("审计RabbitMQ通道已创建，交换机: {ExchangeName}, 队列: {QueueName}",
                _options.ExchangeName, _options.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审计RabbitMQ通道创建失败");
            throw;
        }
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    public Task SendMessageAsync<T>(T message, string? routingKey = null)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ通道未打开");
        }
        
        routingKey ??= _options.RoutingKey;
        
        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            
            // 创建消息属性
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 消息持久化
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            
            // 发布消息
            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);
            
            _logger.LogDebug("审计消息已发送到RabbitMQ: {RoutingKey}, 消息ID: {MessageId}", 
                routingKey, properties.MessageId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送审计消息到RabbitMQ失败");
            throw;
        }
    }
    
    /// <summary>
    /// 订阅消息
    /// </summary>
    public string SubscribeMessage<T>(Func<T, Task> handler, string? routingKey = null)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            throw new InvalidOperationException("RabbitMQ连接未打开");
        }
        
        routingKey ??= _options.RoutingKey;
        
        try
        {
            // 为消费者创建单独的通道
            var consumerChannel = _connection.CreateModel();
            
            // 声明队列
            var queueName = $"{_options.QueueName}.{Guid.NewGuid()}";
            consumerChannel.QueueDeclare(queueName, true, false, true);
            
            // 绑定队列到交换机
            consumerChannel.QueueBind(queueName, _options.ExchangeName, routingKey);
            
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
                    var message = JsonSerializer.Deserialize<T>(json);
                    await handler(message);
                    
                    // 确认消息
                    consumerChannel.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理审计RabbitMQ消息失败");
                    
                    // 拒绝消息并重新入队
                    consumerChannel.BasicNack(e.DeliveryTag, false, true);
                }
            };
            
            // 开始消费
            var consumerTag = consumerChannel.BasicConsume(queueName, false, consumer);
            
            // 保存消费者通道
            _consumerChannels[consumerTag] = consumerChannel;
            
            _logger.LogInformation("审计RabbitMQ消费者已创建，队列: {QueueName}, 消费者标签: {ConsumerTag}",
                queueName, consumerTag);
                
            return consumerTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建审计RabbitMQ消费者失败");
            throw;
        }
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(string consumerTag)
    {
        if (_consumerChannels.TryGetValue(consumerTag, out var channel))
        {
            try
            {
                channel.BasicCancel(consumerTag);
                channel.Dispose();
                _consumerChannels.Remove(consumerTag);
                
                _logger.LogInformation("审计RabbitMQ消费者已取消订阅: {ConsumerTag}", consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消审计RabbitMQ消费者订阅失败: {ConsumerTag}", consumerTag);
            }
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消所有消费者
        foreach (var kvp in _consumerChannels.ToList())
        {
            try
            {
                kvp.Value.BasicCancel(kvp.Key);
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放审计RabbitMQ消费者通道失败: {ConsumerTag}", kvp.Key);
            }
        }
        _consumerChannels.Clear();
        
        // 释放主通道
        try
        {
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "释放审计RabbitMQ主通道失败");
        }
        
        _logger.LogInformation("审计RabbitMQ服务已释放资源");
    }
} 