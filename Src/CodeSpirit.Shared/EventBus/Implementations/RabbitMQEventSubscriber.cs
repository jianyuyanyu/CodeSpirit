using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CodeSpirit.Shared.EventBus.Interfaces;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// RabbitMQ事件订阅者实现
/// </summary>
public class RabbitMQEventSubscriber : RabbitMQEventBusBase, IEventSubscriber
{
    private readonly ILogger<RabbitMQEventSubscriber> _subscriberLogger;
    private readonly Dictionary<string, List<Type>> _eventHandlers;
    private readonly List<string> _queueNames = new();
    private readonly Dictionary<string, string> _consumerTags = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public RabbitMQEventSubscriber(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventSubscriber> logger,
        string exchangeName = "codespirit_event_bus",
        int retryCount = 5)
        : base(connection, serviceProvider, logger, exchangeName, retryCount)
    {
        _subscriberLogger = logger;
        _eventHandlers = new Dictionary<string, List<Type>>();
    }

    /// <summary>
    /// 创建消费者通道
    /// </summary>
    private IModel CreateConsumerChannel()
    {
        if (!_connection.IsOpen)
        {
            _subscriberLogger.LogWarning("RabbitMQ连接已关闭，无法创建通道");
            return null;
        }

        var channel = CreateChannel();
        if (channel == null) return null;

        try
        {
            // 确保交换机存在
            DeclareExchange(channel, _exchangeName);
            DeclareExchange(channel, _deadLetterExchangeName);
            
            // 检查通道状态
            if (!channel.IsOpen)
            {
                _subscriberLogger.LogWarning("声明交换机后通道已关闭，无法继续创建消费者通道");
                return null;
            }

            // 为当前已订阅的事件重新创建队列和绑定
            RecreateQueuesAndBindings(channel);
            
            // 再次检查通道状态
            if (!channel.IsOpen)
            {
                _subscriberLogger.LogWarning("创建队列和绑定后通道已关闭，无法设置QoS");
                return null;
            }

            try
            {
                // 设置预取计数，避免单个消费者过载
                // 确保预取计数正确设置为10
                ushort prefetchCount = 10;
                _subscriberLogger.LogInformation("设置通道QoS: prefetchCount={PrefetchCount}", prefetchCount);
                channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
                
                // 验证QoS是否设置成功
                _subscriberLogger.LogInformation("QoS设置完成，通道ID: {ChannelId}", channel.ChannelNumber);
            }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ex)
            {
                _subscriberLogger.LogWarning(ex, "设置QoS时通道已关闭，将返回null并尝试重连");
                return null;
            }
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogWarning(ex, "创建消费者通道时出错，但将继续使用通道");
            // 检查通道是否仍然开启
            if (channel != null && !channel.IsOpen)
            {
                _subscriberLogger.LogWarning("通道已关闭，返回null");
                return null;
            }
        }

        return channel;
    }

    /// <summary>
    /// 重新创建队列和绑定
    /// </summary>
    private void RecreateQueuesAndBindings(IModel channel)
    {
        // 如果没有事件处理器，直接返回
        if (_eventHandlers.Count == 0)
        {
            _subscriberLogger.LogInformation("没有注册的事件处理器，不需要创建队列");
            return;
        }

        foreach (var eventName in _eventHandlers.Keys)
        {
            try
            {
                var queueName = GetQueueName(eventName);
                CreateQueueAndBindings(channel, queueName, eventName);
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "重新创建队列和绑定失败: {EventName}", eventName);
                // 继续处理下一个事件，不中断循环
            }
        }
    }

    /// <summary>
    /// 获取队列名称
    /// </summary>
    private string GetQueueName(string eventName) => $"codespirit_queue_{eventName}";

    /// <summary>
    /// 创建队列和绑定
    /// </summary>
    private void CreateQueueAndBindings(IModel channel, string queueName, string eventName)
    {
        // 检查通道状态
        if (channel == null || !channel.IsOpen)
        {
            _subscriberLogger.LogWarning("无法创建队列和绑定：通道已关闭或为空，队列：{QueueName}", queueName);
            return;
        }

        try
        {
            bool queueExists = CheckQueueExists(channel, queueName);

            // 如果通道检查后被关闭，则退出
            if (!channel.IsOpen)
            {
                _subscriberLogger.LogWarning("检查队列后通道已关闭，无法继续处理，队列：{QueueName}", queueName);
                return;
            }

            if (!queueExists)
            {
                CreateQueue(channel, queueName, eventName);
            }

            // 再次检查通道是否开启
            if (!channel.IsOpen)
            {
                _subscriberLogger.LogWarning("创建队列后通道已关闭，无法继续绑定，队列：{QueueName}", queueName);
                return;
            }

            try
            {
                // 绑定队列到交换机
                channel.QueueBind(
                    queue: queueName,
                    exchange: _exchangeName,
                    routingKey: eventName);

                // 创建消费者
                CreateConsumer(channel, queueName, eventName);

                _subscriberLogger.LogInformation("成功绑定队列: {QueueName} 到交换机: {ExchangeName}", queueName, _exchangeName);
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "绑定队列到交换机失败: {QueueName}", queueName);
                
                // 如果是通道关闭异常，标记通道为关闭状态
                if (ex is RabbitMQ.Client.Exceptions.AlreadyClosedException)
                {
                    _subscriberLogger.LogWarning("通道在绑定过程中关闭，将在后台尝试重连");
                    Task.Run(() => TryConnectWithRetry(CreateConsumerChannel));
                }
            }
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "创建队列和绑定失败: {QueueName}", queueName);
        }
    }

    /// <summary>
    /// 检查队列是否存在
    /// </summary>
    private bool CheckQueueExists(IModel channel, string queueName)
    {
        // 检查通道状态
        if (channel == null || !channel.IsOpen)
        {
            _subscriberLogger.LogWarning("无法检查队列是否存在：通道已关闭或为空，队列：{QueueName}", queueName);
            return false;
        }

        try
        {
            channel.QueueDeclarePassive(queueName);
            _subscriberLogger.LogInformation("队列已存在: {QueueName}", queueName);
            return true;
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
        {
            // 404表示队列不存在，这是正常的，不算错误
            _subscriberLogger.LogInformation("队列不存在，将创建新队列: {QueueName}", queueName);
            return false;
        }
        catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ex)
        {
            _subscriberLogger.LogWarning(ex, "检查队列时通道已关闭，将在后台尝试重连: {QueueName}", queueName);
            Task.Run(() => TryConnectWithRetry(CreateConsumerChannel));
            return false;
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "检查队列是否存在时发生错误: {QueueName}", queueName);
            return false;
        }
    }

    /// <summary>
    /// 创建队列
    /// </summary>
    private void CreateQueue(IModel channel, string queueName, string eventName)
    {
        // 检查通道状态
        if (channel == null || !channel.IsOpen)
        {
            _subscriberLogger.LogWarning("创建队列失败：通道已关闭或为空，队列：{QueueName}", queueName);
            return;
        }

        try
        {
            var arguments = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", _deadLetterExchangeName},
                {"x-dead-letter-routing-key", eventName}
            };

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _subscriberLogger.LogInformation("成功创建新队列: {QueueName}, durable: true，routingKey：{routingKey}", queueName, eventName);
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "创建队列失败: {QueueName}", queueName);
            
            // 如果是通道关闭异常，标记通道为关闭状态
            if (ex is RabbitMQ.Client.Exceptions.AlreadyClosedException)
            {
                _subscriberLogger.LogWarning("通道已关闭，将在后台尝试重连");
                Task.Run(() => TryConnectWithRetry(CreateConsumerChannel));
            }
            
            // 不抛出异常，让调用方处理
        }
    }

    /// <summary>
    /// 创建消费者
    /// </summary>
    private void CreateConsumer(IModel channel, string queueName, string eventName)
    {
        // 检查通道状态
        if (channel == null || !channel.IsOpen)
        {
            _subscriberLogger.LogWarning("无法创建消费者：通道已关闭或为空，队列：{QueueName}", queueName);
            return;
        }

        try
        {
            // 确保设置了正确的QoS
            try
            {
                ushort prefetchCount = 10;
                _subscriberLogger.LogInformation("设置QoS: prefetchCount={PrefetchCount}", prefetchCount);
                channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
                
                // 验证QoS是否设置成功
                _subscriberLogger.LogInformation("QoS设置完成，通道ID: {ChannelId}", channel.ChannelNumber);
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "设置QoS失败，可能会影响消费者性能");
                // 继续创建消费者，不中断流程
            }

            // 如果已经存在消费者，先取消订阅
            if (_consumerTags.TryGetValue(queueName, out var oldConsumerTag))
            {
                try
                {
                    _subscriberLogger.LogInformation("取消已存在的消费者: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, oldConsumerTag);
                    channel.BasicCancel(oldConsumerTag);
                    _consumerTags.Remove(queueName);
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogWarning(ex, "取消已有消费者时出错: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, oldConsumerTag);
                    // 继续创建新消费者
                }
            }

            // 创建消费者配置
            var consumerConfig = new ConsumerConfiguration(channel)
            {
                ConsumerTag = $"codespirit_consumer_{Guid.NewGuid():N}",
                NoAck = false, // 必须手动确认
                NoLocal = false,
                Exclusive = false,
                Arguments = null
            };
            
            // 获取队列详情，验证队列状态
            try
            {
                var queueDeclareResult = channel.QueueDeclarePassive(queueName);
                _subscriberLogger.LogInformation("队列状态: {QueueName}, MessageCount: {MessageCount}, ConsumerCount: {ConsumerCount}", 
                    queueName, queueDeclareResult.MessageCount, queueDeclareResult.ConsumerCount);
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "获取队列状态失败: {QueueName}", queueName);
            }
            
            // 创建消费者
            var consumer = new AsyncEventingBasicConsumer(channel);
            
            // 添加调试输出
            consumer.ConsumerCancelled += (sender, args) => {
                _subscriberLogger.LogWarning("消费者被取消: {QueueName}", queueName);
                return Task.CompletedTask;
            };
            
            consumer.Shutdown += (sender, args) => {
                _subscriberLogger.LogWarning("消费者关闭: {QueueName}, ReplyText: {ReplyText}", queueName, args.ReplyText);
                return Task.CompletedTask;
            };
            
            // 收到消息的处理
            consumer.Received += async (sender, args) => {
                try
                {
                    _subscriberLogger.LogInformation($"{eventName} 收到消息，DeliveryTag: {args.DeliveryTag}, Exchange: {args.Exchange}, RoutingKey: {args.RoutingKey}");
                    await HandleReceivedMessageAsync(channel, eventName, args);
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogError(ex, "Received事件处理器异常: {QueueName}, DeliveryTag: {DeliveryTag}", queueName, args.DeliveryTag);
                    try
                    {
                        // 出现异常时，避免消息丢失，重新入队
                        channel.BasicNack(args.DeliveryTag, false, true);
                        _subscriberLogger.LogWarning("由于异常，消息已重新入队: {QueueName}, DeliveryTag: {DeliveryTag}", queueName, args.DeliveryTag);
                    }
                    catch
                    {
                        // 忽略拒绝消息时的异常
                    }
                }
            };

            // 使用显式的ConsumerTag
            string consumerTag = channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumerTag: consumerConfig.ConsumerTag,
                noLocal: consumerConfig.NoLocal,
                exclusive: consumerConfig.Exclusive,
                arguments: consumerConfig.Arguments,
                consumer: consumer);
            
            // 保存消费者标签
            _consumerTags[queueName] = consumerTag;
            
            _subscriberLogger.LogInformation("成功创建队列 {QueueName} 的消费者，标签: {ConsumerTag}, QoS预取数: 10, 通道ID: {ChannelId}", 
                queueName, consumerTag, channel.ChannelNumber);
        }
        catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ex)
        {
            _subscriberLogger.LogWarning(ex, "创建消费者时通道已关闭，将在后台尝试重连: {QueueName}", queueName);
            Task.Run(() => TryConnectWithRetry(CreateConsumerChannel));
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "创建消费者失败: {QueueName}", queueName);
        }
    }

    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private async Task HandleReceivedMessageAsync(IModel channel, string eventName, BasicDeliverEventArgs args)
    {
        bool success = false;
        string messageId = args.BasicProperties?.MessageId ?? "未知ID";
        string messageBody = string.Empty;
        
        try
        {
            messageBody = Encoding.UTF8.GetString(args.Body.ToArray());
            _subscriberLogger.LogInformation("收到消息: {EventName} - {MessageId}, DeliveryTag: {DeliveryTag}, Exchange: {Exchange}, RoutingKey: {RoutingKey}", 
                eventName, messageId, args.DeliveryTag, args.Exchange, args.RoutingKey);
            
            // 输出消息内容，方便诊断
            _subscriberLogger.LogInformation("消息内容: {MessageBody}", messageBody);
            
            // 查找事件类型
            var eventType = FindEventType(eventName);
            if (eventType == null)
            {
                _subscriberLogger.LogError("无法找到事件类型: {EventName}, 消息将被拒绝", eventName);
                success = false;
            }
            else
            {
                // 处理事件
                success = await ProcessEventAsync(eventName, messageBody);
                
                // 记录处理结果
                if (success)
                {
                    _subscriberLogger.LogInformation("成功处理消息: {EventName} - {MessageId}", eventName, messageId);
                }
                else
                {
                    _subscriberLogger.LogWarning("处理消息失败: {EventName} - {MessageId}", eventName, messageId);
                }
            }
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "处理事件 {EventName} 消息 {MessageId} 时出错", eventName, messageId);
        }
        finally
        {
            try
            {
                // 根据处理结果决定确认或拒绝消息
                if (success)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    _subscriberLogger.LogInformation("已确认消息: {EventName} - {MessageId}, DeliveryTag: {DeliveryTag}", 
                        eventName, messageId, args.DeliveryTag);
                }
                else
                {
                    // 判断是否应该重新入队 - 如果找不到事件类型或消息体解析失败，不重新入队，避免无限循环
                    bool requeue = eventName != null && !string.IsNullOrEmpty(messageBody);
                    
                    // 如果重新入队，记录日志
                    if (requeue)
                    {
                        _subscriberLogger.LogWarning("消息将重新入队: {EventName} - {MessageId}, DeliveryTag: {DeliveryTag}", 
                            eventName, messageId, args.DeliveryTag);
                    }
                    else
                    {
                        _subscriberLogger.LogWarning("消息将被拒绝且不重新入队: {EventName} - {MessageId}, DeliveryTag: {DeliveryTag}", 
                            eventName, messageId, args.DeliveryTag);
                    }
                    
                    // 拒绝消息
                    channel.BasicNack(args.DeliveryTag, false, requeue);
                }
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "确认/拒绝消息时发生错误: {EventName} - {MessageId}, DeliveryTag: {DeliveryTag}", 
                    eventName, messageId, args.DeliveryTag);
            }
        }
    }

    /// <summary>
    /// 处理通过BasicGet获取的消息
    /// </summary>
    private async Task HandleReceivedMessageAsync(IModel channel, string eventName, BasicGetResult result)
    {
        // 转换为BasicDeliverEventArgs格式
        var args = new BasicDeliverEventArgs(
            "", // BasicGet没有ConsumerTag
            result.DeliveryTag,
            result.Redelivered,
            result.Exchange,
            result.RoutingKey,
            result.BasicProperties,
            result.Body
        );
        
        // 调用标准处理方法
        await HandleReceivedMessageAsync(channel, eventName, args);
    }

    /// <summary>
    /// 连接丢失时的处理
    /// </summary>
    protected override void OnConnectionLost()
    {
        _subscriberLogger.LogWarning("RabbitMQ连接丢失，将尝试重新连接并恢复订阅");
        
        // 防止多线程并发操作
        lock (_lockObj)
        {
            // 清空消费者标签，避免重复创建消费者
            _consumerTags.Clear();
            
            if (_channel != null)
            {
                try
                {
                    _channel.Dispose();
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogWarning(ex, "释放已关闭的通道时发生错误");
                }
                _channel = null;
            }
        }
        
        // 尝试重新连接
        Task.Run(async () => {
            try
            {
                // 随机延迟1-3秒，避免所有客户端同时重连
                var random = new Random();
                int delayMs = random.Next(1000, 3000);
                await Task.Delay(delayMs);
                _subscriberLogger.LogInformation("开始尝试重连，延迟了{Delay}毫秒", delayMs);
                
                // 如果连接关闭，等待自动恢复
                if (!_connection.IsOpen)
                {
                    _subscriberLogger.LogWarning("等待RabbitMQ连接自动恢复");
                    // 等待连接恢复，最多等待30秒
                    for (int i = 0; i < 30; i++)
                    {
                        if (_connection.IsOpen)
                        {
                            _subscriberLogger.LogInformation("RabbitMQ连接已自动恢复");
                            break;
                        }
                        await Task.Delay(1000); // 等待1秒
                    }
                }
                
                // 如果连接仍未恢复，无法继续
                if (!_connection.IsOpen)
                {
                    _subscriberLogger.LogError("在等待30秒后RabbitMQ连接仍未恢复");
                    return;
                }
                
                // 清理旧消费者
                await PurgeAllConsumersAsync();
                
                // 重新恢复队列和绑定
                foreach (var eventEntry in _eventHandlers.ToList())
                {
                    var eventName = eventEntry.Key;
                    var queueName = GetQueueName(eventName);
                    
                    try
                    {
                        _subscriberLogger.LogInformation("正在重新创建队列和绑定: {QueueName}", queueName);
                        await EnsureQueueDeclaredAndBoundAsync(queueName, eventName);
                    }
                    catch (Exception ex)
                    {
                        _subscriberLogger.LogError(ex, "重新创建队列和绑定失败: {QueueName}", queueName);
                    }
                }
                
                _subscriberLogger.LogInformation("RabbitMQ连接和队列恢复完成");
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "尝试恢复RabbitMQ连接时出错");
            }
        });
    }

    /// <summary>
    /// 清除所有现有消费者
    /// </summary>
    private async Task PurgeAllConsumersAsync()
    {
        _subscriberLogger.LogInformation("开始清除所有现有消费者");
        
        // 获取消费者列表的副本，避免遍历时修改集合
        var consumerTags = _consumerTags.ToList();
        
        foreach (var kvp in consumerTags)
        {
            string queueName = kvp.Key;
            string consumerTag = kvp.Value;
            
            try
            {
                // 确保创建了通道
                await EnsureChannelAsync(CreateConsumerChannel);
                
                if (_channel != null && _channel.IsOpen)
                {
                    _subscriberLogger.LogInformation("正在取消消费者: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, consumerTag);
                    _channel.BasicCancel(consumerTag);
                    _subscriberLogger.LogInformation("已成功取消消费者: {QueueName}", queueName);
                }
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "取消消费者 {QueueName}, ConsumerTag: {ConsumerTag} 时出错", queueName, consumerTag);
            }
        }
        
        // 清空字典
        _consumerTags.Clear();
        _subscriberLogger.LogInformation("已清除所有消费者记录");
    }

    /// <summary>
    /// 确保队列已声明并绑定到交换机
    /// </summary>
    private async Task EnsureQueueDeclaredAndBoundAsync(string queueName, string routingKey)
    {
        int maxRetries = 3;
        int attempt = 0;
        bool success = false;
        
        _subscriberLogger.LogInformation("开始确保队列 {QueueName} 已声明并绑定", queueName);

        // 先尝试清除该队列的旧消费者
        if (_consumerTags.TryGetValue(queueName, out var oldConsumerTag))
        {
            try
            {
                if (_channel != null && _channel.IsOpen)
                {
                    _subscriberLogger.LogInformation("绑定前清除旧消费者: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, oldConsumerTag);
                    _channel.BasicCancel(oldConsumerTag);
                    _consumerTags.Remove(queueName);
                }
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "绑定前清除旧消费者失败: {QueueName}", queueName);
            }
        }

        while (!success && attempt < maxRetries && !_disposed)
        {
            try
            {
                // 检查连接状态
                if (!_connection.IsOpen)
                {
                    attempt++;
                    _subscriberLogger.LogWarning("RabbitMQ连接未打开，等待重连 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                    
                    // 等待连接恢复
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
                    continue;
                }

                // 创建新的通道专门用于队列声明和绑定
                using var newChannel = _connection.CreateModel();
                if (newChannel == null)
                {
                    attempt++;
                    _subscriberLogger.LogWarning("无法创建RabbitMQ通道，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                // 声明交换机
                _subscriberLogger.LogInformation("声明交换机: {ExchangeName}", _exchangeName);
                newChannel.ExchangeDeclare(
                    exchange: _exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // 声明死信交换机
                _subscriberLogger.LogInformation("声明死信交换机: {DeadLetterExchangeName}", _deadLetterExchangeName);
                newChannel.ExchangeDeclare(
                    exchange: _deadLetterExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // 创建队列
                _subscriberLogger.LogInformation("声明队列: {QueueName}", queueName);
                var arguments = new Dictionary<string, object>
                {
                    {"x-dead-letter-exchange", _deadLetterExchangeName},
                    {"x-dead-letter-routing-key", routingKey}
                };

                newChannel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: arguments);

                // 绑定队列到交换机
                _subscriberLogger.LogInformation("绑定队列 {QueueName} 到交换机 {ExchangeName} 使用路由键 {RoutingKey}", 
                    queueName, _exchangeName, routingKey);
                newChannel.QueueBind(
                    queue: queueName,
                    exchange: _exchangeName,
                    routingKey: routingKey);

                // 记录队列名称
                if (!_queueNames.Contains(queueName))
                {
                    _queueNames.Add(queueName);
                }

                _subscriberLogger.LogInformation("队列 {QueueName} 已成功声明并绑定到交换机", queueName);
                success = true;
                
                // 使用新的通道为队列创建消费者
                await EnsureConsumerCreatedAsync(queueName, routingKey);
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                attempt++;
                _subscriberLogger.LogWarning(ex, "RabbitMQ服务器不可达，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
            }
            catch (RabbitMQ.Client.Exceptions.AlreadyClosedException ex)
            {
                attempt++;
                _subscriberLogger.LogWarning(ex, "RabbitMQ通道已关闭，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
            }
            catch (Exception ex)
            {
                attempt++;
                _subscriberLogger.LogError(ex, "队列声明或绑定过程中发生错误，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
            }
        }

        if (!success)
        {
            _subscriberLogger.LogError("在 {MaxRetries} 次尝试后仍无法声明和绑定队列 {QueueName}", maxRetries, queueName);
        }
    }

    /// <summary>
    /// 确保为队列创建消费者
    /// </summary>
    private async Task EnsureConsumerCreatedAsync(string queueName, string routingKey)
    {
        int maxRetries = 3;
        int attempt = 0;
        bool success = false;
        
        _subscriberLogger.LogInformation("开始为队列 {QueueName} 创建消费者", queueName);

        while (!success && attempt < maxRetries && !_disposed)
        {
            try
            {
                // 检查连接和通道状态
                if (!_connection.IsOpen)
                {
                    attempt++;
                    _subscriberLogger.LogWarning("RabbitMQ连接未打开，无法创建消费者，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
                    continue;
                }

                // 使用主通道创建消费者
                await EnsureChannelAsync(CreateConsumerChannel);
                
                if (_channel == null || _channel.IsClosed)
                {
                    attempt++;
                    _subscriberLogger.LogWarning("主通道无效或已关闭，无法创建消费者，等待重试 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                // 确保设置了正确的QoS
                try
                {
                    ushort prefetchCount = 10;
                    _subscriberLogger.LogInformation("设置QoS: prefetchCount={PrefetchCount}", prefetchCount);
                    _channel.BasicQos(prefetchSize: 0, prefetchCount: prefetchCount, global: false);
                    
                    // 验证QoS设置
                    _subscriberLogger.LogInformation("QoS设置完成，通道ID: {ChannelId}", _channel.ChannelNumber);
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogWarning(ex, "设置QoS失败，可能会影响消费者性能");
                    // 继续创建消费者，不中断流程
                }

                // 检查是否已有消费者，有则清理
                if (_consumerTags.TryGetValue(queueName, out var oldConsumerTag))
                {
                    try
                    {
                        _subscriberLogger.LogInformation("清理已存在的消费者: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, oldConsumerTag);
                        _channel.BasicCancel(oldConsumerTag);
                        _consumerTags.Remove(queueName);
                    }
                    catch (Exception ex)
                    {
                        _subscriberLogger.LogWarning(ex, "清理已有消费者时出错: {QueueName}, ConsumerTag: {ConsumerTag}", queueName, oldConsumerTag);
                        // 即使出错也继续创建新消费者
                    }
                }

                // 获取队列详情，验证队列状态
                try
                {
                    var queueDeclareResult = _channel.QueueDeclarePassive(queueName);
                    _subscriberLogger.LogInformation("队列状态: {QueueName}, MessageCount: {MessageCount}, ConsumerCount: {ConsumerCount}", 
                        queueName, queueDeclareResult.MessageCount, queueDeclareResult.ConsumerCount);
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogWarning(ex, "获取队列状态失败: {QueueName}", queueName);
                }

                // 创建消费者配置
                var consumerConfig = new ConsumerConfiguration(_channel)
                {
                    ConsumerTag = $"codespirit_consumer_{Guid.NewGuid():N}",
                    NoAck = false, // 必须手动确认
                    NoLocal = false,
                    Exclusive = false,
                    Arguments = null
                };

                // 为队列创建消费者
                var consumer = new AsyncEventingBasicConsumer(_channel);
                
                // 添加调试输出
                consumer.ConsumerCancelled += (sender, args) => {
                    _subscriberLogger.LogWarning("消费者被取消: {QueueName}", queueName);
                    return Task.CompletedTask;
                };
                
                consumer.Shutdown += (sender, args) => {
                    _subscriberLogger.LogWarning("消费者关闭: {QueueName}, ReplyText: {ReplyText}", queueName, args.ReplyText);
                    return Task.CompletedTask;
                };
                
                // 消息接收处理
                consumer.Received += async (sender, args) => {
                    try 
                    {
                        _subscriberLogger.LogInformation($"{routingKey} 收到消息，DeliveryTag: {args.DeliveryTag}, Exchange: {args.Exchange}, RoutingKey: {args.RoutingKey}");
                        await HandleReceivedMessageAsync(_channel, routingKey, args);
                    }
                    catch (Exception ex)
                    {
                        _subscriberLogger.LogError(ex, "Received事件处理器异常: {QueueName}, DeliveryTag: {DeliveryTag}", queueName, args.DeliveryTag);
                        try
                        {
                            // 出现异常时，避免消息丢失，重新入队
                            _channel.BasicNack(args.DeliveryTag, false, true);
                            _subscriberLogger.LogWarning("由于异常，消息已重新入队: {QueueName}, DeliveryTag: {DeliveryTag}", queueName, args.DeliveryTag);
                        }
                        catch
                        {
                            // 忽略拒绝消息时的异常
                        }
                    }
                };

                // 使用显式的ConsumerTag
                string consumerTag = _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumerTag: consumerConfig.ConsumerTag,
                    noLocal: consumerConfig.NoLocal,
                    exclusive: consumerConfig.Exclusive,
                    arguments: consumerConfig.Arguments,
                    consumer: consumer);
                
                // 保存消费者标签
                _consumerTags[queueName] = consumerTag;

                _subscriberLogger.LogInformation("已成功为队列 {QueueName} 创建消费者，标签: {ConsumerTag}, QoS预取数: 10, 通道ID: {ChannelId}", 
                    queueName, consumerTag, _channel.ChannelNumber);
                    
                // 测试从队列主动获取一条消息
                try
                {
                    _subscriberLogger.LogInformation("尝试从队列手动获取一条消息: {QueueName}", queueName);
                    var result = _channel.BasicGet(queueName, false);
                    if (result != null)
                    {
                        _subscriberLogger.LogInformation("成功手动获取消息: DeliveryTag={DeliveryTag}, Exchange={Exchange}, RoutingKey={RoutingKey}", 
                            result.DeliveryTag, result.Exchange, result.RoutingKey);
                            
                        // 处理消息
                        await HandleReceivedMessageAsync(_channel, routingKey, result);
                    }
                    else
                    {
                        _subscriberLogger.LogInformation("队列中没有可获取的消息: {QueueName}", queueName);
                    }
                }
                catch (Exception ex)
                {
                    _subscriberLogger.LogWarning(ex, "尝试手动获取消息失败: {QueueName}", queueName);
                }
                    
                success = true;
            }
            catch (Exception ex)
            {
                attempt++;
                _subscriberLogger.LogError(ex, "为队列 {QueueName} 创建消费者时发生错误，等待重试 (尝试 {Attempt}/{MaxRetries})", queueName, attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))));
            }
        }

        if (!success)
        {
            _subscriberLogger.LogError("在 {MaxRetries} 次尝试后仍无法为队列 {QueueName} 创建消费者", maxRetries, queueName);
        }
    }

    /// <summary>
    /// 注册事件处理器
    /// </summary>
    private void RegisterEventHandler(string eventName, Type handlerType)
    {
        if (!_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers.Add(eventName, new List<Type>());
            _subscriberLogger.LogInformation("创建事件 {EventName} 的处理器列表", eventName);
        }

        if (_eventHandlers[eventName].Contains(handlerType))
        {
            _subscriberLogger.LogWarning("处理器类型 {HandlerType} 已经订阅事件 {EventName}", handlerType.Name, eventName);
            return;
        }

        _eventHandlers[eventName].Add(handlerType);
        _subscriberLogger.LogInformation("事件 {EventName} 添加处理器 {HandlerType}", eventName, handlerType.Name);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    public void Unsubscribe<TEvent, THandler>()
        where THandler : IEventHandler<TEvent>
    {
        if (_disposed)
        {
            _subscriberLogger.LogWarning("事件订阅者已被释放，无法取消订阅事件");
            return;
        }

        var eventName = typeof(TEvent).Name;
        var handlerType = typeof(THandler);

        if (!_eventHandlers.ContainsKey(eventName)) return;

        _eventHandlers[eventName].Remove(handlerType);

        if (_eventHandlers[eventName].Count == 0)
        {
            _eventHandlers.Remove(eventName);
            UnbindQueue(eventName);
        }
    }

    /// <summary>
    /// 解绑队列
    /// </summary>
    private void UnbindQueue(string eventName)
    {
        try
        {
            // 确保通道可用
            if (_channel == null || _channel.IsClosed)
            {
                _subscriberLogger.LogWarning("RabbitMQ通道已关闭，无法取消绑定队列");
                return;
            }

            // 解绑队列
            var queueName = GetQueueName(eventName);
            if (_queueNames.Contains(queueName))
            {
                // 如果有消费者，先取消
                if (_consumerTags.TryGetValue(queueName, out var consumerTag))
                {
                    try
                    {
                        _channel.BasicCancel(consumerTag);
                        _subscriberLogger.LogInformation("已取消队列 {QueueName} 的消费者，标签: {ConsumerTag}", queueName, consumerTag);
                        _consumerTags.Remove(queueName);
                    }
                    catch (Exception ex)
                    {
                        _subscriberLogger.LogWarning(ex, "取消消费者时出错: {QueueName}, {ConsumerTag}", queueName, consumerTag);
                    }
                }

                _channel.QueueUnbind(
                    queue: queueName,
                    exchange: _exchangeName,
                    routingKey: eventName);

                _queueNames.Remove(queueName);
                _subscriberLogger.LogInformation("已取消绑定队列: {QueueName}, 事件: {EventName}", queueName, eventName);
            }
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "取消订阅事件失败: {EventName}", eventName);
        }
    }

    /// <summary>
    /// 处理事件
    /// </summary>
    private async Task<bool> ProcessEventAsync(string eventName, string message)
    {
        _subscriberLogger.LogInformation("开始处理事件: {EventName}", eventName);

        if (_disposed)
        {
            _subscriberLogger.LogWarning("事件订阅者已被释放，无法处理事件");
            return false;
        }

        if (!_eventHandlers.ContainsKey(eventName))
        {
            _subscriberLogger.LogWarning("没有找到事件 {EventName} 的处理器", eventName);
            return false;
        }

        bool processedSuccessfully = true;

        using var scope = _serviceProvider.CreateScope();
        foreach (var handlerType in _eventHandlers[eventName])
        {
            try
            {
                var handler = scope.ServiceProvider.GetService(handlerType);
                if (handler == null)
                {
                    _subscriberLogger.LogWarning("无法解析处理器类型: {HandlerType}", handlerType.Name);
                    processedSuccessfully = false;
                    continue;
                }

                // 查找事件类型
                var eventType = FindEventType(eventName);
                if (eventType == null)
                {
                    _subscriberLogger.LogError("无法找到事件类型: {EventName}", eventName);
                    processedSuccessfully = false;
                    continue;
                }

                // 反序列化事件
                object @event = DeserializeEvent(message, eventType);
                if (@event == null)
                {
                    processedSuccessfully = false;
                    continue;
                }

                // 获取处理方法
                var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                var method = concreteType.GetMethod("HandleAsync");

                if (method == null)
                {
                    _subscriberLogger.LogError("处理器 {HandlerType} 没有实现HandleAsync方法", handlerType.Name);
                    processedSuccessfully = false;
                    continue;
                }

                // 使用重试机制调用处理方法
                if (!await InvokeHandlerWithRetryAsync(handler, method, @event, eventName, handlerType.Name))
                {
                    processedSuccessfully = false;
                }
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "处理事件失败: {EventName}, 处理器: {HandlerType}", eventName, handlerType.Name);
                processedSuccessfully = false;
            }
        }

        return processedSuccessfully;
    }

    /// <summary>
    /// 查找事件类型
    /// </summary>
    private Type FindEventType(string eventName)
    {
        // 使用缓存提高性能
        Type cachedType = TypeCache.GetOrAdd(eventName, name => {
            // 更全面的事件类型查找
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => 
                {
                    try 
                    { 
                        return a.GetTypes(); 
                    }
                    catch 
                    { 
                        return new Type[0]; 
                    }
                })
                .FirstOrDefault(t => t.Name == name);
            
            if (type == null)
            {
                _subscriberLogger.LogWarning("无法找到事件类型: {EventName}，检查所有程序集", eventName);
                
                // 扩展查找，检查可能的命名空间
                string[] potentialNamespaces = new[] 
                { 
                    "CodeSpirit.Shared.Events",
                    "CodeSpirit.Events", 
                    "CodeSpirit",
                    "Events"
                };
                
                foreach (var ns in potentialNamespaces)
                {
                    try
                    {
                        var fullTypeName = $"{ns}.{eventName}";
                        type = Type.GetType(fullTypeName);
                        if (type != null)
                        {
                            _subscriberLogger.LogInformation("在命名空间 {Namespace} 中找到事件类型: {EventName}", ns, eventName);
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略查找错误，继续尝试下一个命名空间
                    }
                }
            }
            
            return type;
        });
        
        if (cachedType == null)
        {
            _subscriberLogger.LogError("最终无法找到事件类型: {EventName}, 消息将无法处理", eventName);
        }
        else
        {
            _subscriberLogger.LogDebug("找到事件类型: {EventName} -> {TypeFullName}", eventName, cachedType.FullName);
        }
        
        return cachedType;
    }

    /// <summary>
    /// 事件类型缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new ConcurrentDictionary<string, Type>();

    /// <summary>
    /// 反序列化事件
    /// </summary>
    private object DeserializeEvent(string message, Type eventType)
    {
        try
        {
            // 尝试使用不同的序列化选项
            try
            {
                var @event = JsonConvert.DeserializeObject(message, eventType, _jsonSettings);
                if (@event != null)
                {
                    return @event;
                }
                
                _subscriberLogger.LogWarning("使用默认设置反序列化消息失败: {EventType}，尝试其他方式", eventType.Name);
            }
            catch (JsonException ex)
            {
                _subscriberLogger.LogWarning(ex, "使用默认设置反序列化JSON失败: {EventType}，尝试其他方式", eventType.Name);
            }
            
            // 尝试使用更宽松的设置
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Populate,
                DateParseHandling = DateParseHandling.DateTime,
                FloatParseHandling = FloatParseHandling.Double,
                DateTimeZoneHandling = DateTimeZoneHandling.Local
            };
            
            var @fallbackEvent = JsonConvert.DeserializeObject(message, eventType, settings);
            if (@fallbackEvent == null)
            {
                _subscriberLogger.LogError("无法反序列化消息为事件类型: {EventType}，使用宽松设置也失败", eventType.Name);
                return null;
            }
            
            _subscriberLogger.LogInformation("使用宽松设置成功反序列化消息: {EventType}", eventType.Name);
            return @fallbackEvent;
        }
        catch (JsonException ex)
        {
            _subscriberLogger.LogError(ex, "JSON反序列化失败: {EventType}", eventType.Name);
            return null;
        }
        catch (Exception ex)
        {
            _subscriberLogger.LogError(ex, "反序列化过程中发生错误: {EventType}", eventType.Name);
            return null;
        }
    }

    /// <summary>
    /// 消费者配置
    /// </summary>
    private class ConsumerConfiguration
    {
        public string ConsumerTag { get; set; }
        public bool NoAck { get; set; }
        public bool NoLocal { get; set; }
        public bool Exclusive { get; set; }
        public IDictionary<string, object> Arguments { get; set; }
        public IModel Channel { get; }

        public ConsumerConfiguration(IModel channel)
        {
            Channel = channel;
        }
    }

    /// <summary>
    /// 使用重试机制调用处理方法
    /// </summary>
    private async Task<bool> InvokeHandlerWithRetryAsync(object handler, System.Reflection.MethodInfo method, object @event, string eventName, string handlerName)
    {
        int maxRetries = 3;
        int attempt = 0;
        bool succeeded = false;

        var stopwatch = Stopwatch.StartNew();

        while (!succeeded && attempt < maxRetries)
        {
            try
            {
                await (Task)method.Invoke(handler, new[] { @event });
                succeeded = true;
            }
            catch (Exception ex)
            {
                attempt++;

                // 判断是否继续重试
                if (attempt >= maxRetries || ex is ArgumentException)
                {
                    _subscriberLogger.LogError(ex, "所有重试后处理事件仍然失败: {EventName}, 处理器: {HandlerName}",
                        eventName, handlerName);
                    break;
                }

                var waitTime = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _subscriberLogger.LogWarning(ex, "处理事件失败，重试 {RetryCount}/{MaxRetry} 将在 {TimeSpan} 秒后进行",
                    attempt, maxRetries, waitTime.TotalSeconds);

                await Task.Delay(waitTime);
            }
        }

        if (succeeded)
        {
            _subscriberLogger.LogInformation("已处理事件: {EventName}, 处理器: {HandlerName}", eventName, handlerName);
        }

        stopwatch.Stop();
        _subscriberLogger.LogInformation("消息处理耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return succeeded;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public override void Dispose()
    {
        if (_disposed) return;

        lock (_lockObj)
        {
            if (_disposed) return;

            try
            {
                UnbindAllQueues();
                base.Dispose();
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogError(ex, "释放订阅者资源时出错");
            }
        }
    }

    /// <summary>
    /// 解绑所有队列
    /// </summary>
    private void UnbindAllQueues()
    {
        // 取消所有消费者
        foreach (var queueConsumer in _consumerTags.ToList())
        {
            try
            {
                string queueName = queueConsumer.Key;
                string consumerTag = queueConsumer.Value;
                
                _channel?.BasicCancel(consumerTag);
                _subscriberLogger.LogInformation("已取消队列 {QueueName} 的消费者，标签: {ConsumerTag}", queueName, consumerTag);
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "取消消费者 {ConsumerTag} 时发生错误", queueConsumer.Value);
            }
        }
        _consumerTags.Clear();

        foreach (var queueName in _queueNames)
        {
            try
            {
                _channel?.QueueUnbind(queueName, _exchangeName, "*");
            }
            catch (Exception ex)
            {
                _subscriberLogger.LogWarning(ex, "解绑队列 {QueueName} 时发生错误", queueName);
            }
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    public async Task Subscribe<TEvent, THandler>()
        where THandler : IEventHandler<TEvent>
    {
        if (_disposed)
        {
            _subscriberLogger.LogError("事件订阅者已被释放，无法订阅事件");
            throw new ObjectDisposedException(nameof(RabbitMQEventSubscriber));
        }

        var eventName = typeof(TEvent).Name;
        var handlerType = typeof(THandler);

        // 添加事件处理器
        RegisterEventHandler(eventName, handlerType);

        var queueName = GetQueueName(eventName);
        
        // 使用更可靠的方式尝试创建队列和绑定
        await EnsureQueueDeclaredAndBoundAsync(queueName, eventName);

        _subscriberLogger.LogInformation("已订阅事件: {EventName}, 处理器: {HandlerType}", eventName, handlerType.Name);
    }
}