using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.ServiceDefaults.Messaging;

/// <summary>
/// RabbitMQ服务工厂实现
/// 基于Aspire.RabbitMQ.Client的键控客户端管理不同用途的连接
/// </summary>
public class RabbitMQServiceFactory : IRabbitMQServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQServiceFactory> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    public RabbitMQServiceFactory(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQServiceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取事件总线连接
    /// </summary>
    /// <returns>事件总线专用连接</returns>
    public RabbitMQ.Client.IConnection GetEventBusConnection()
    {
        return GetConnection("eventbus");
    }

    /// <summary>
    /// 获取审计服务连接
    /// </summary>
    /// <returns>审计服务专用连接</returns>
    public RabbitMQ.Client.IConnection GetAuditConnection()
    {
        return GetConnection("audit");
    }

    /// <summary>
    /// 获取通用消息连接
    /// </summary>
    /// <returns>通用消息专用连接</returns>
    public RabbitMQ.Client.IConnection GetMessagingConnection()
    {
        return GetConnection("messaging");
    }

    /// <summary>
    /// 获取指定键的连接
    /// </summary>
    /// <param name="connectionKey">连接键</param>
    /// <returns>指定键的连接</returns>
    public RabbitMQ.Client.IConnection GetConnection(string connectionKey)
    {
        try
        {
            var connection = _serviceProvider.GetKeyedService<RabbitMQ.Client.IConnection>(connectionKey);
            if (connection == null)
            {
                _logger.LogWarning("未找到键为 {ConnectionKey} 的RabbitMQ连接，尝试使用默认连接", connectionKey);
                // 如果找不到指定的键控连接，尝试获取默认连接
                connection = _serviceProvider.GetService<RabbitMQ.Client.IConnection>();
            }

            if (connection == null)
            {
                throw new InvalidOperationException($"无法获取RabbitMQ连接，连接键: {connectionKey}");
            }

            _logger.LogDebug("成功获取RabbitMQ连接，连接键: {ConnectionKey}, 连接状态: {IsOpen}", 
                connectionKey, connection.IsOpen);
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取RabbitMQ连接失败，连接键: {ConnectionKey}", connectionKey);
            throw;
        }
    }
} 