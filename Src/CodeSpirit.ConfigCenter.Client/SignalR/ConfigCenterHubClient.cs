using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.ConfigCenter.Client.SignalR;

/// <summary>
/// 配置中心Hub客户端
/// </summary>
public class ConfigCenterHubClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ConfigCenterClientOptions _options;
    private readonly ILogger<ConfigCenterHubClient> _logger;
    private bool _isConnected;

    public event Func<Task> OnConfigChanged;

    public ConfigCenterHubClient(
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCenterHubClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 创建Hub连接
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_options.ServiceUrl}/config-hub")
            .WithAutomaticReconnect()
            .Build();

        // 注册配置变更处理程序
        _hubConnection.On("ConfigChanged", async () =>
        {
            _logger.LogInformation("接收到应用 {AppId} 在 {Environment} 环境的配置变更通知", 
                _options.AppId, _options.Environment);
            
            if (OnConfigChanged != null)
            {
                await OnConfigChanged.Invoke();
            }
        });

        // 注册连接状态变更处理程序
        _hubConnection.Closed += async (error) =>
        {
            _isConnected = false;
            _logger.LogWarning(error, "与配置中心的SignalR连接已关闭");
            
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await ConnectAsync();
        };
    }

    /// <summary>
    /// 连接到Hub
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_isConnected)
        {
            return;
        }

        try
        {
            await _hubConnection.StartAsync();
            _isConnected = true;
            
            _logger.LogInformation("已成功连接到配置中心SignalR Hub");
            
            // 注册配置监听
            await _hubConnection.InvokeAsync("RegisterAppConfigListener", 
                _options.AppId, _options.Environment);
            
            _logger.LogInformation("已注册应用 {AppId} 在 {Environment} 环境的配置监听", 
                _options.AppId, _options.Environment);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, "连接到配置中心SignalR Hub失败：{Message}", ex.Message);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isConnected)
        {
            try
            {
                // 取消注册配置监听
                await _hubConnection.InvokeAsync("UnregisterAppConfigListener", 
                    _options.AppId, _options.Environment);
                
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭SignalR连接时发生错误：{Message}", ex.Message);
            }
        }
    }
} 