using CodeSpirit.Audit.Models.LLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CodeSpirit.Audit.Services.LLM.Implementation;

/// <summary>
/// LLM审计消费者后台服务
/// </summary>
public class LLMAuditConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMAuditConsumerService> _logger;
    private readonly LLMAuditOptions _options;
    private readonly IConnection? _rabbitMqConnection;
    private IChannel? _channel;
    
    /// <summary>
    /// 初始化LLM审计消费者服务
    /// </summary>
    public LLMAuditConsumerService(
        IServiceProvider serviceProvider,
        ILogger<LLMAuditConsumerService> logger,
        IOptions<Models.AuditOptions> auditOptions,
        IConnection? rabbitMqConnection = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit;
        _rabbitMqConnection = rabbitMqConnection;
    }
    
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("LLM审计已禁用，消费者服务不会启动");
            return;
        }
        
        if (_rabbitMqConnection == null)
        {
            _logger.LogWarning("RabbitMQ连接未配置，LLM审计消费者服务不会启动");
            return;
        }
        
        try
        {
            _channel = _rabbitMqConnection.CreateChannelAsync().GetAwaiter().GetResult();
            
            // 声明交换机和队列（确保它们存在）
            InitializeRabbitMQInfrastructure();
            
            // 设置预取数量（RabbitMQ.Client 7.x 使用异步方法）
            _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false).GetAwaiter().GetResult();
            
            // 创建消费者（RabbitMQ.Client 7.x 使用 AsyncEventingBasicConsumer）
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogDebug("收到LLM审计日志消息");
                    
                    var auditLog = JsonSerializer.Deserialize<LLMAuditLog>(message);
                    if (auditLog != null)
                    {
                        // 使用作用域服务处理消息
                        using var scope = _serviceProvider.CreateScope();
                        var storageService = scope.ServiceProvider.GetRequiredService<ILLMAuditStorageService>();
                        
                        var success = await storageService.StoreAsync(auditLog);
                        
                        if (success)
                        {
                            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            _logger.LogDebug("LLM审计日志已成功存储并确认: {Id}", auditLog.Id);
                        }
                        else
                        {
                            // 存储失败，拒绝消息并重新入队
                            await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                            _logger.LogWarning("LLM审计日志存储失败，消息已重新入队: {Id}", auditLog.Id);
                        }
                    }
                    else
                    {
                        _logger.LogError("无法反序列化LLM审计日志消息");
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理LLM审计日志消息时发生异常");
                    
                    // 异常情况下也确认消息，避免死循环
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
            
            _channel.BasicConsumeAsync(
                queue: _options.RabbitMQ.QueueName,
                autoAck: false,
                consumer: consumer).GetAwaiter().GetResult();
            
            _logger.LogInformation("LLM审计消费者服务已启动，监听队列: {QueueName}", _options.RabbitMQ.QueueName);
            
            // 初始化存储
            using var initScope = _serviceProvider.CreateScope();
            var storageService = initScope.ServiceProvider.GetRequiredService<ILLMAuditStorageService>();
            await storageService.InitializeAsync();
            
            // 等待取消信号
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LLM审计消费者服务正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM审计消费者服务发生异常");
        }
    }
    
    /// <summary>
    /// 初始化RabbitMQ基础设施（交换机、队列、绑定）
    /// </summary>
    private void InitializeRabbitMQInfrastructure()
    {
        try
        {
            // 声明交换机（RabbitMQ.Client 7.x 使用异步方法）
            _channel.ExchangeDeclareAsync(
                exchange: _options.RabbitMQ.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();
            
            // 声明队列
            _channel.QueueDeclareAsync(
                queue: _options.RabbitMQ.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false).GetAwaiter().GetResult();
            
            // 绑定队列到交换机
            _channel.QueueBindAsync(
                queue: _options.RabbitMQ.QueueName,
                exchange: _options.RabbitMQ.ExchangeName,
                routingKey: _options.RabbitMQ.RoutingKey).GetAwaiter().GetResult();
            
            _logger.LogInformation("LLM审计消费者RabbitMQ基础设施初始化成功 - 交换机: {Exchange}, 队列: {Queue}", 
                _options.RabbitMQ.ExchangeName, _options.RabbitMQ.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化LLM审计消费者RabbitMQ基础设施失败");
            throw;
        }
    }
    
    /// <inheritdoc/>
    public override void Dispose()
    {
        // RabbitMQ.Client 7.x 中 IChannel 没有 Close 方法，直接 Dispose
        _channel?.Dispose();
        base.Dispose();
        
        _logger.LogInformation("LLM审计消费者服务已释放资源");
    }
}

