using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// RabbitMQ服务实现
/// </summary>
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQ.Client.IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly RabbitMQOptions _options;
    private readonly Dictionary<string, IModel> _consumerChannels = new Dictionary<string, IModel>();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public RabbitMQService(ILogger<RabbitMQService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // 获取配置
        var options = new AuditOptions();
        configuration.GetSection("Audit").Bind(options);
        _options = options.RabbitMQ;
        
        try
        {
            // 创建连接
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true // 使用异步消费者
            };
            
            _connection = factory.CreateConnection();
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
            
            _logger.LogInformation("RabbitMQ连接已建立");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ连接建立失败");
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
            
            // 发布消息
            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                basicProperties: null,
                body: body);
            
            _logger.LogDebug("消息已发送到RabbitMQ: {RoutingKey}", routingKey);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息到RabbitMQ失败");
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
                    _logger.LogError(ex, "处理RabbitMQ消息失败");
                    
                    // 拒绝消息并重新入队
                    consumerChannel.BasicNack(e.DeliveryTag, false, true);
                }
            };
            
            // 开始消费
            var consumerTag = consumerChannel.BasicConsume(queueName, false, consumer);
            
            // 保存消费者通道
            _consumerChannels[consumerTag] = consumerChannel;
            
            _logger.LogInformation("已订阅RabbitMQ消息: {RoutingKey}", routingKey);
            return consumerTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅RabbitMQ消息失败");
            throw;
        }
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(string consumerTag)
    {
        if (string.IsNullOrEmpty(consumerTag) || !_consumerChannels.ContainsKey(consumerTag))
        {
            return;
        }
        
        try
        {
            var channel = _consumerChannels[consumerTag];
            channel.BasicCancel(consumerTag);
            channel.Close();
            _consumerChannels.Remove(consumerTag);
            
            _logger.LogInformation("已取消订阅RabbitMQ消息: {ConsumerTag}", consumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅RabbitMQ消息失败");
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            // 关闭所有消费者通道
            foreach (var channel in _consumerChannels.Values)
            {
                if (channel.IsOpen)
                {
                    channel.Close();
                }
            }
            
            // 关闭主通道
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
            }
            
            // 关闭连接
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
            }
            
            _logger.LogInformation("RabbitMQ连接已关闭");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭RabbitMQ连接失败");
        }
    }
} 