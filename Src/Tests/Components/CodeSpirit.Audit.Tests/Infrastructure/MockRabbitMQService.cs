using CodeSpirit.Audit.Services;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 模拟的RabbitMQ服务，用于测试
/// </summary>
public class MockRabbitMQService : IRabbitMQService
{
    private readonly ILogger<MockRabbitMQService> _logger;
    
    public MockRabbitMQService(ILogger<MockRabbitMQService> logger)
    {
        _logger = logger;
        _logger.LogInformation("使用模拟的RabbitMQ服务");
    }
    
    public Task SendMessageAsync<T>(T message, string? routingKey = null)
    {
        _logger.LogInformation("发送消息: {Message}, 路由键: {RoutingKey}", message, routingKey);
        return Task.CompletedTask;
    }
    
    public string SubscribeMessage<T>(Func<T, Task> handler, string? routingKey = null)
    {
        _logger.LogInformation("订阅消息, 路由键: {RoutingKey}", routingKey);
        return Guid.NewGuid().ToString();
    }
    
    public void Unsubscribe(string consumerTag)
    {
        _logger.LogInformation("取消订阅: {ConsumerTag}", consumerTag);
    }
    
    public void Dispose()
    {
        _logger.LogInformation("释放模拟的RabbitMQ服务");
    }
} 