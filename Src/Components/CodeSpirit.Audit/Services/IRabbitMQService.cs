namespace CodeSpirit.Audit.Services;

/// <summary>
/// RabbitMQ服务接口
/// </summary>
public interface IRabbitMQService
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="message">消息内容</param>
    /// <param name="routingKey">路由键，可选</param>
    /// <returns>任务</returns>
    Task SendMessageAsync<T>(T message, string? routingKey = null);
    
    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <typeparam name="T">消息类型</typeparam>
    /// <param name="handler">消息处理方法</param>
    /// <param name="routingKey">路由键，可选</param>
    /// <returns>消费者标识</returns>
    string SubscribeMessage<T>(Func<T, Task> handler, string? routingKey = null);
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <param name="consumerTag">消费者标识</param>
    void Unsubscribe(string consumerTag);
} 