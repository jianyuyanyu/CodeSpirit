using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using CodeSpirit.Audit.Models;
using CodeSpirit.ServiceDefaults.Messaging;
using System.Collections.Concurrent;

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
    private readonly ConcurrentDictionary<string, IModel> _consumerChannels = new();
    private readonly object _subscriptionLock = new object();
    private readonly JsonSerializerOptions _jsonOptions;
    
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
        
        // 获取配置 - 智能处理配置绑定
        var options = new AuditOptions();
        if (configuration.GetSection("Audit").Exists())
        {
            // 传入的是完整配置，获取Audit节
            configuration.GetSection("Audit").Bind(options);
        }
        else
        {
            // 传入的就是Audit配置节
            configuration.Bind(options);
        }
        _options = options.RabbitMQ;
        
        // 配置JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
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
            
            _logger.LogInformation("队列声明完成: {QueueName}, durable=true, exclusive=false, autoDelete=false", 
                _options.QueueName);

            // 绑定队列到交换机
            _channel.QueueBind(
                queue: _options.QueueName,
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey);
            
            _logger.LogInformation("队列绑定完成: 队列={Queue} -> 交换机={Exchange}, 路由键={RoutingKey}",
                _options.QueueName, _options.ExchangeName, _options.RoutingKey);
            
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
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(json);
            
            _logger.LogDebug("准备发送消息，交换机: {Exchange}, 路由键: {RoutingKey}, 消息大小: {Size} bytes", 
                _options.ExchangeName, routingKey, body.Length);
            
            // 创建消息属性
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // 消息持久化
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";
            
            // 发布消息
            _channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);
            
            _logger.LogDebug("审计消息已发送到RabbitMQ: 交换机={Exchange}, 路由键={RoutingKey}, 消息ID={MessageId}", 
                _options.ExchangeName, routingKey, properties.MessageId);
            
            // 验证消息是否真正发布（仅调试时使用）
            _logger.LogDebug("消息发布完成，JSON内容: {Json}", json.Length > 1000 ? json.Substring(0, 1000) + "..." : json);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送审计消息到RabbitMQ失败，交换机: {Exchange}, 路由键: {RoutingKey}", 
                _options.ExchangeName, routingKey);
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
        
        _logger.LogInformation("=== 开始创建RabbitMQ消费者 ===");
        _logger.LogInformation("连接状态: {IsOpen}, 路由键: {RoutingKey}", _connection.IsOpen, routingKey);
        _logger.LogInformation("配置信息 - 交换机: {Exchange}, 队列: {Queue}, 默认路由键: {DefaultRouting}", 
            _options.ExchangeName, _options.QueueName, _options.RoutingKey);
        
        // 使用锁确保线程安全
        lock (_subscriptionLock)
        {
            IModel? consumerChannel = null;
            try
            {
                _logger.LogInformation("开始创建审计RabbitMQ消费者，路由键: {RoutingKey}", routingKey);
                
                // 为消费者创建单独的通道
                consumerChannel = _connection.CreateModel();
                _logger.LogInformation("消费者通道创建成功，通道号: {ChannelNumber}, 是否打开: {IsOpen}", 
                    consumerChannel.ChannelNumber, consumerChannel.IsOpen);
                
                // 确保队列存在（在消费者通道中重新声明）
                _logger.LogInformation("在消费者通道中重新声明队列和绑定...");
                try
                {
                    consumerChannel.QueueDeclare(
                        queue: _options.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false);
                    
                    // 绑定默认路由键
                    consumerChannel.QueueBind(
                        queue: _options.QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: _options.RoutingKey);
                    
                    // 如果传入的路由键与默认不同，绑定额外的路由键
                    if (routingKey != _options.RoutingKey)
                    {
                        _logger.LogInformation("绑定额外的路由键: {RoutingKey}", routingKey);
                        consumerChannel.QueueBind(
                            queue: _options.QueueName,
                            exchange: _options.ExchangeName,
                            routingKey: routingKey);
                    }
                    
                    _logger.LogInformation("消费者通道中队列声明和绑定完成");
                }
                catch (Exception queueEx)
                {
                    _logger.LogError(queueEx, "消费者通道中队列声明或绑定失败");
                    throw;
                }
                
                // 设置QoS
                _logger.LogDebug("设置消费者QoS...");
                consumerChannel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
                _logger.LogDebug("QoS设置完成");
                
                // 创建消费者
                _logger.LogInformation("创建EventingBasicConsumer...");
                var consumer = new EventingBasicConsumer(consumerChannel);
                _logger.LogInformation("消费者对象创建成功，类型: {Type}", consumer.GetType().Name);
                
                // 注册消息接收事件
                _logger.LogInformation("注册消息接收事件处理器...");
                consumer.Received += (sender, e) =>
                {
                    var body = e.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation("=== 收到RabbitMQ消息 ===");
                    _logger.LogInformation("DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                    _logger.LogInformation("Exchange: {Exchange}", e.Exchange);
                    _logger.LogInformation("RoutingKey: {RoutingKey}", e.RoutingKey);
                    _logger.LogInformation("消息大小: {Size} bytes", body.Length);
                    _logger.LogInformation("消息内容预览: {Preview}", json.Length > 200 ? json.Substring(0, 200) + "..." : json);
                    
                    try
                    {
                        _logger.LogDebug("开始反序列化消息...");
                        var message = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                        if (message != null)
                        {
                            _logger.LogInformation("消息反序列化成功，开始处理...");
                            
                            // 同步调用处理器
                            handler(message).Wait();
                            
                            // 确认消息
                            consumerChannel.BasicAck(e.DeliveryTag, false);
                            _logger.LogInformation("=== 消息处理完成 === DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                        }
                        else
                        {
                            _logger.LogWarning("消息反序列化结果为null，DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                            consumerChannel.BasicNack(e.DeliveryTag, false, false); // 不重新入队
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "消息反序列化失败，DeliveryTag: {DeliveryTag}, 消息内容: {Json}", 
                            e.DeliveryTag, json);
                        // 序列化错误的消息不重新入队
                        consumerChannel.BasicNack(e.DeliveryTag, false, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理审计RabbitMQ消息失败，DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                        
                        // 业务处理错误，重新入队重试
                        consumerChannel.BasicNack(e.DeliveryTag, false, true);
                    }
                };
                _logger.LogInformation("事件处理器注册完成");
                
                // 开始消费
                _logger.LogInformation("调用BasicConsume开始消费，队列: {Queue}, autoAck: false", _options.QueueName);
                var consumerTag = consumerChannel.BasicConsume(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumer: consumer);
                
                _logger.LogInformation("BasicConsume调用成功，返回消费者标签: {ConsumerTag}", consumerTag);
                
                // 保存消费者通道（线程安全）
                _consumerChannels[consumerTag] = consumerChannel;
                _logger.LogInformation("消费者通道已保存到字典，当前消费者数量: {Count}", _consumerChannels.Count);
                
                _logger.LogInformation("=== 审计RabbitMQ消费者创建完成 ===");
                _logger.LogInformation("队列: {QueueName}, 消费者标签: {ConsumerTag}, 路由键: {RoutingKey}",
                    _options.QueueName, consumerTag, routingKey);
                    
                return consumerTag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== 创建审计RabbitMQ消费者失败 ===");
                _logger.LogError("异常类型: {ExceptionType}, 消息: {Message}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("内部异常: {InnerException}", ex.InnerException.Message);
                }
                _logger.LogError("堆栈跟踪: {StackTrace}", ex.StackTrace);
                
                // 清理资源
                try
                {
                    consumerChannel?.Dispose();
                    _logger.LogInformation("已清理失败的消费者通道");
                }
                catch (Exception disposeEx)
                {
                    _logger.LogWarning(disposeEx, "释放消费者通道时出错");
                }
                
                throw;
            }
        }
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe(string consumerTag)
    {
        if (_consumerChannels.TryRemove(consumerTag, out var channel))
        {
            try
            {
                channel.BasicCancel(consumerTag);
                channel.Dispose();
                
                _logger.LogInformation("审计RabbitMQ消费者已取消订阅: {ConsumerTag}", consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消审计RabbitMQ消费者订阅失败: {ConsumerTag}", consumerTag);
            }
        }
        else
        {
            _logger.LogWarning("未找到消费者标签: {ConsumerTag}", consumerTag);
        }
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        lock (_subscriptionLock)
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
} 