using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Helpers;
using CodeSpirit.ServiceDefaults.Messaging;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// RabbitMQ服务实现
/// 基于Aspire.RabbitMQ.Client集成重构，支持线程安全的通道池
/// </summary>
public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQ.Client.IConnection _connection;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly RabbitMQOptions _options;
    private readonly ConcurrentDictionary<string, IChannel> _consumerChannels = new();
    private readonly object _subscriptionLock = new object();
    
    // 线程安全的通道池
    private readonly ConcurrentQueue<IChannel> _channelPool = new();
    private readonly SemaphoreSlim _channelSemaphore;
    private readonly int _maxChannels = 10;
    
    // 订阅限制
    private readonly int _maxSubscribers;
    private volatile bool _disposed = false;
    
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
        
        // 使用配置助手简化配置绑定
        _options = ConfigurationHelper.BindRabbitMQOptions(configuration);
        
        // 初始化通道池信号量
        _channelSemaphore = new SemaphoreSlim(_maxChannels, _maxChannels);
        
        // 设置最大订阅者数量（从配置读取，默认为3）
        _maxSubscribers = _options.MaxSubscribers > 0 ? _options.MaxSubscribers : 3;
        
        
        try
        {
            // 初始化通道池
            InitializeChannelPool();
            
            _logger.LogInformation("审计RabbitMQ服务已初始化，通道池大小: {MaxChannels}, 最大订阅者: {MaxSubscribers}, 交换机: {ExchangeName}, 队列: {QueueName}",
                _maxChannels, _maxSubscribers, _options.ExchangeName, _options.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审计RabbitMQ服务初始化失败");
            throw;
        }
    }
    
    /// <summary>
    /// 初始化通道池
    /// </summary>
    private void InitializeChannelPool()
    {
        // 预创建一些通道
        for (int i = 0; i < Math.Min(3, _maxChannels); i++)
        {
            var channel = CreateAndConfigureChannel();
            if (channel != null)
            {
                _channelPool.Enqueue(channel);
            }
        }
    }
    
    /// <summary>
    /// 创建并配置通道
    /// </summary>
    private IChannel? CreateAndConfigureChannel()
    {
        try
        {
            var channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            
                    // 声明交换机（RabbitMQ.Client 7.x 使用异步方法）
                    channel.ExchangeDeclareAsync(
                        exchange: _options.ExchangeName,
                        type: ExchangeType.Topic,
                        durable: true,
                        autoDelete: false).GetAwaiter().GetResult();
                    
                    // 声明队列
                    channel.QueueDeclareAsync(
                        queue: _options.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false).GetAwaiter().GetResult();
                    
                    // 绑定队列到交换机
                    channel.QueueBindAsync(
                        queue: _options.QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: _options.RoutingKey).GetAwaiter().GetResult();
            
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建RabbitMQ通道失败");
            return null;
        }
    }
    
    /// <summary>
    /// 获取通道（线程安全）
    /// </summary>
    private async Task<IChannel?> GetChannelAsync()
    {
        if (_disposed)
            return null;
            
        await _channelSemaphore.WaitAsync();
        
        try
        {
            // 尝试从池中获取可用通道
            if (_channelPool.TryDequeue(out var channel))
            {
                if (channel.IsOpen)
                {
                    return channel;
                }
                else
                {
                    // 通道已关闭，释放资源并创建新的
                    try { channel.Dispose(); } catch { }
                }
            }
            
            // 创建新通道
            return CreateAndConfigureChannel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取RabbitMQ通道失败");
            return null;
        }
    }
    
    /// <summary>
    /// 归还通道到池中
    /// </summary>
    private void ReturnChannel(IChannel channel)
    {
        try
        {
            if (!_disposed && channel != null && channel.IsOpen && _channelPool.Count < _maxChannels)
            {
                _channelPool.Enqueue(channel);
            }
            else
            {
                // 池已满或通道无效，直接释放
                try { channel?.Dispose(); } catch { }
            }
        }
        finally
        {
            _channelSemaphore.Release();
        }
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync<T>(T message, string? routingKey = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQService));
            
        var channel = await GetChannelAsync();
        if (channel == null)
        {
            throw new InvalidOperationException("无法获取RabbitMQ通道");
        }
        
        routingKey ??= _options.RoutingKey;
        
        try
        {
            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            
            _logger.LogDebug("准备发送消息，交换机: {Exchange}, 路由键: {RoutingKey}, 消息大小: {Size} bytes", 
                _options.ExchangeName, routingKey, body.Length);
            
            // 创建消息属性（RabbitMQ.Client 7.x 直接实例化 BasicProperties）
            var properties = new BasicProperties
            {
                Persistent = true, // 消息持久化
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };
            
            // 发布消息（RabbitMQ.Client 7.x 使用异步方法）
            channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: new ReadOnlyMemory<byte>(body)).GetAwaiter().GetResult();
            
            _logger.LogDebug("审计消息已发送到RabbitMQ: 交换机={Exchange}, 路由键={RoutingKey}, 消息ID={MessageId}", 
                _options.ExchangeName, routingKey, properties.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送审计消息到RabbitMQ失败，交换机: {Exchange}, 路由键: {RoutingKey}", 
                _options.ExchangeName, routingKey);
            
            // 通道可能已损坏，不归还到池中
            try { channel.Dispose(); } catch { }
            _channelSemaphore.Release();
            throw;
        }
        
        // 归还通道到池中
        ReturnChannel(channel);
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
        
        // 检查订阅者数量限制
        if (_consumerChannels.Count >= _maxSubscribers)
        {
            _logger.LogWarning("已达到最大订阅者数量限制: {MaxSubscribers}，当前订阅者: {CurrentCount}", 
                _maxSubscribers, _consumerChannels.Count);
            throw new InvalidOperationException($"已达到最大订阅者数量限制: {_maxSubscribers}");
        }
        
        routingKey ??= _options.RoutingKey;
        
        _logger.LogInformation("=== 开始创建RabbitMQ消费者 ===");
        _logger.LogInformation("连接状态: {IsOpen}, 路由键: {RoutingKey}", _connection.IsOpen, routingKey);
        _logger.LogInformation("配置信息 - 交换机: {Exchange}, 队列: {Queue}, 默认路由键: {DefaultRouting}", 
            _options.ExchangeName, _options.QueueName, _options.RoutingKey);
        
        // 使用锁确保线程安全
        lock (_subscriptionLock)
        {
            IChannel? consumerChannel = null;
            try
            {
                _logger.LogInformation("开始创建审计RabbitMQ消费者，路由键: {RoutingKey}", routingKey);
                
                // 为消费者创建单独的通道（RabbitMQ.Client 7.x 使用异步方法）
                consumerChannel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                _logger.LogInformation("消费者通道创建成功，通道号: {ChannelNumber}, 是否打开: {IsOpen}", 
                    consumerChannel.ChannelNumber, consumerChannel.IsOpen);
                
                // 确保队列存在（在消费者通道中重新声明）
                _logger.LogInformation("在消费者通道中重新声明队列和绑定...");
                try
                {
                    consumerChannel.QueueDeclareAsync(
                        queue: _options.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false).GetAwaiter().GetResult();
                    
                    // 绑定默认路由键
                    consumerChannel.QueueBindAsync(
                        queue: _options.QueueName,
                        exchange: _options.ExchangeName,
                        routingKey: _options.RoutingKey).GetAwaiter().GetResult();
                    
                    // 如果传入的路由键与默认不同，绑定额外的路由键
                    if (routingKey != _options.RoutingKey)
                    {
                        _logger.LogInformation("绑定额外的路由键: {RoutingKey}", routingKey);
                        consumerChannel.QueueBindAsync(
                            queue: _options.QueueName,
                            exchange: _options.ExchangeName,
                            routingKey: routingKey).GetAwaiter().GetResult();
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
                consumerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false).GetAwaiter().GetResult();
                _logger.LogDebug("QoS设置完成");
                
                // 创建消费者（RabbitMQ.Client 7.x 使用 AsyncEventingBasicConsumer）
                _logger.LogInformation("创建AsyncEventingBasicConsumer...");
                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                _logger.LogInformation("消费者对象创建成功，类型: {Type}", consumer.GetType().Name);
                
                // 注册消息接收事件（RabbitMQ.Client 7.x 使用 ReceivedAsync 事件）
                _logger.LogInformation("注册消息接收事件处理器...");
                consumer.ReceivedAsync += async (sender, e) =>
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
                        var message = JsonConvert.DeserializeObject<T>(json);
                        if (message != null)
                        {
                            _logger.LogInformation("消息反序列化成功，开始处理...");
                            
                            // 异步调用处理器
                            await handler(message);
                            
                            // 确认消息（RabbitMQ.Client 7.x 使用异步方法）
                            await consumerChannel.BasicAckAsync(e.DeliveryTag, false);
                            _logger.LogInformation("=== 消息处理完成 === DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                        }
                        else
                        {
                            _logger.LogWarning("消息反序列化结果为null，DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                            await consumerChannel.BasicNackAsync(e.DeliveryTag, false, false); // 不重新入队
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "消息反序列化失败，DeliveryTag: {DeliveryTag}, 消息内容: {Json}", 
                            e.DeliveryTag, json);
                        // 序列化错误的消息不重新入队
                        await consumerChannel.BasicNackAsync(e.DeliveryTag, false, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理审计RabbitMQ消息失败，DeliveryTag: {DeliveryTag}", e.DeliveryTag);
                        
                        // 业务处理错误，重新入队重试
                        await consumerChannel.BasicNackAsync(e.DeliveryTag, false, true);
                    }
                };
                _logger.LogInformation("事件处理器注册完成");
                
                // 开始消费
                _logger.LogInformation("调用BasicConsume开始消费，队列: {Queue}, autoAck: false", _options.QueueName);
                var consumerTag = consumerChannel.BasicConsumeAsync(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumer: consumer).GetAwaiter().GetResult();
                
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
    /// 获取订阅者状态信息
    /// </summary>
    /// <returns>订阅者状态信息</returns>
    public (int CurrentSubscribers, int MaxSubscribers, bool CanSubscribe) GetSubscriberStatus()
    {
        var currentCount = _consumerChannels.Count;
        var canSubscribe = currentCount < _maxSubscribers && !_disposed;
        
        return (currentCount, _maxSubscribers, canSubscribe);
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
                channel.BasicCancelAsync(consumerTag).GetAwaiter().GetResult();
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
                    kvp.Value.BasicCancelAsync(kvp.Key).GetAwaiter().GetResult();
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
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放审计RabbitMQ主通道失败");
            }
            
            _logger.LogInformation("审计RabbitMQ服务已释放资源");
        }
        
        _disposed = true;
    }
} 