namespace CodeSpirit.ServiceDefaults.Messaging;

/// <summary>
/// RabbitMQ服务工厂接口
/// 提供不同用途的RabbitMQ连接管理
/// </summary>
public interface IRabbitMQServiceFactory
{
    /// <summary>
    /// 获取事件总线连接
    /// </summary>
    /// <returns>事件总线专用连接</returns>
    RabbitMQ.Client.IConnection GetEventBusConnection();

    /// <summary>
    /// 获取审计服务连接
    /// </summary>
    /// <returns>审计服务专用连接</returns>
    RabbitMQ.Client.IConnection GetAuditConnection();

    /// <summary>
    /// 获取通用消息连接
    /// </summary>
    /// <returns>通用消息专用连接</returns>
    RabbitMQ.Client.IConnection GetMessagingConnection();

    /// <summary>
    /// 获取指定键的连接
    /// </summary>
    /// <param name="connectionKey">连接键</param>
    /// <returns>指定键的连接</returns>
    RabbitMQ.Client.IConnection GetConnection(string connectionKey);
} 