using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using CodeSpirit.Shared.EventBus.Interfaces;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CodeSpirit.Shared.EventBus.Implementations;

/// <summary>
/// RabbitMQ事件总线基类
/// </summary>
public abstract class RabbitMQEventBusBase : IDisposable
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    protected readonly IConnection _connection;
    protected IModel _channel;
    protected readonly string _exchangeName;
    protected readonly string _deadLetterExchangeName;
    protected readonly int _retryCount;
    protected bool _disposed;
    protected readonly object _lockObj = new();
    protected readonly JsonSerializerSettings _jsonSettings;

    /// <summary>
    /// 构造函数
    /// </summary>
    protected RabbitMQEventBusBase(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger logger,
        string exchangeName = "codespirit_event_bus",
        int retryCount = 5)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exchangeName = !string.IsNullOrEmpty(exchangeName) ? exchangeName : throw new ArgumentNullException(nameof(exchangeName));
        _deadLetterExchangeName = $"{_exchangeName}.dlx";
        _retryCount = retryCount;
        _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        RegisterConnectionEventHandlers();
    }

    /// <summary>
    /// 注册连接事件处理程序
    /// </summary>
    protected void RegisterConnectionEventHandlers()
    {
        _connection.ConnectionShutdown += OnConnectionShutdown;
        _connection.CallbackException += OnCallbackException;
        _connection.ConnectionBlocked += OnConnectionBlocked;
    }

    /// <summary>
    /// 创建通道
    /// </summary>
    protected IModel CreateChannel()
    {
        if (!_connection.IsOpen)
        {
            _logger.LogWarning("RabbitMQ连接已关闭，无法创建通道");
            return null;
        }

        IModel channel = null;
        try
        {
            channel = _connection.CreateModel();

            // 声明主交换机和死信交换机
            try
            {
                // 声明主交换机
                DeclareExchange(channel, _exchangeName);
                
                // 声明死信交换机
                DeclareExchange(channel, _deadLetterExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "声明交换机时出错，但将继续使用通道");
                // 不抛出异常，继续使用已创建的通道
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建RabbitMQ通道时出错");
            channel?.Dispose();
            return null;
        }

        return channel;
    }

    /// <summary>
    /// 声明交换机
    /// </summary>
    protected void DeclareExchange(IModel channel, string exchangeName)
    {
        try
        {
            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            
            _logger.LogDebug("成功声明交换机: {ExchangeName}", exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "声明交换机 {ExchangeName} 时出错，将尝试以被动方式检查交换机是否存在", exchangeName);
            
            try
            {
                // 尝试以被动方式检查交换机是否存在
                channel.ExchangeDeclarePassive(exchangeName);
                _logger.LogInformation("交换机已存在: {ExchangeName}", exchangeName);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "交换机不存在且无法创建: {ExchangeName}", exchangeName);
                // 不抛出异常，让应用继续运行
            }
        }
    }

    /// <summary>
    /// 尝试重新连接
    /// </summary>
    protected void TryConnectWithRetry(Func<IModel> channelFactory)
    {
        Task.Run(async () => {
            int attempt = 0;
            bool success = false;

            while (attempt < _retryCount && !success && !_disposed)
            {
                try
                {
                    IModel newChannel = null;
                    
                    // 避免在锁内部执行耗时操作
                    try
                    {
                        newChannel = channelFactory();
                    }
                    catch (Exception channelEx)
                    {
                        _logger.LogWarning(channelEx, "通过工厂方法创建通道失败（尝试 {Attempt}/{RetryCount}）", 
                            attempt + 1, _retryCount);
                        newChannel = null;
                    }
                    
                    // 仅在获取到有效通道时进行更新
                    if (newChannel != null && newChannel.IsOpen)
                    {
                        lock (_lockObj)
                        {
                            if (_disposed) 
                            {
                                newChannel.Dispose();
                                return;
                            }

                            // 释放旧通道
                            _channel?.Dispose();
                            _channel = newChannel;
                        }
                        
                        _logger.LogInformation("RabbitMQ连接恢复成功（尝试 {Attempt}/{RetryCount}）", 
                            attempt + 1, _retryCount);
                        success = true;
                        break;
                    }
                    else if (newChannel != null)
                    {
                        // 如果通道已经关闭，释放它
                        newChannel.Dispose();
                    }
                }
                catch (Exception ex) when (ex is BrokerUnreachableException || ex is SocketException)
                {
                    attempt++;
                    var waitTime = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))); // 最长等待30秒
                    _logger.LogWarning(ex, "RabbitMQ连接失败（尝试 {Attempt}/{RetryCount}），{TimeSpan}秒后重试...",
                        attempt, _retryCount, waitTime.TotalSeconds);

                    await Task.Delay(waitTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ连接失败，无法恢复连接");
                    break;
                }
                
                // 增加尝试次数并等待
                if (!success && attempt < _retryCount)
                {
                    attempt++;
                    var waitTime = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))); // 最长等待30秒
                    _logger.LogInformation("RabbitMQ连接重试（尝试 {Attempt}/{RetryCount}）将在{TimeSpan}秒后进行",
                        attempt, _retryCount, waitTime.TotalSeconds);
                    
                    await Task.Delay(waitTime);
                }
            }

            if (!success && attempt >= _retryCount)
            {
                _logger.LogError("尝试重连RabbitMQ {RetryCount} 次后失败", _retryCount);
            }
        });
    }

    /// <summary>
    /// 连接关闭事件处理
    /// </summary>
    protected void OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ连接已关闭: {Reason}", e.ReplyText);
        OnConnectionLost();
    }

    /// <summary>
    /// 连接异常事件处理
    /// </summary>
    protected void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        _logger.LogWarning("RabbitMQ回调异常: {Exception}", e.Exception.Message);
        OnConnectionLost();
    }

    /// <summary>
    /// 连接阻塞事件处理
    /// </summary>
    protected void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ连接被阻塞: {Reason}", e.Reason);
    }

    /// <summary>
    /// 连接丢失时的处理
    /// </summary>
    protected abstract void OnConnectionLost();

    /// <summary>
    /// 确保通道可用
    /// </summary>
    protected async Task EnsureChannelAsync(Func<IModel> channelFactory)
    {
        if (_channel == null || _channel.IsClosed)
        {
            try
            {
                await Task.Run(() => {
                    lock (_lockObj)
                    {
                        _channel?.Dispose();
                        
                        try 
                        {
                            _channel = channelFactory();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "通过工厂方法创建RabbitMQ通道失败");
                            _channel = null;
                        }
                        
                        // 不再抛出异常，而是让调用者检查_channel是否为null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保通道可用时发生异常");
                // 不再抛出异常，让调用者处理_channel为null的情况
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed) return;

        lock (_lockObj)
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _channel?.Close();
                _channel?.Dispose();
                _channel = null;

                UnregisterConnectionEventHandlers();

                _logger.LogInformation("RabbitMQ事件总线已成功释放资源");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放RabbitMQ资源时出错");
            }
        }
    }

    /// <summary>
    /// 注销连接事件处理程序
    /// </summary>
    protected void UnregisterConnectionEventHandlers()
    {
        _connection.ConnectionShutdown -= OnConnectionShutdown;
        _connection.CallbackException -= OnCallbackException;
        _connection.ConnectionBlocked -= OnConnectionBlocked;
    }
} 