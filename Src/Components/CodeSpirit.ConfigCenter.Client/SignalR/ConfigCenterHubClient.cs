using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

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
    private string _clientId;

    public event Func<Task> OnConfigChanged;

    public ConfigCenterHubClient(
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCenterHubClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _clientId = GenerateClientId();

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
            
            // 发送客户端信息
            await RegisterClientInfoAsync();
            
            // 注册配置监听
            await _hubConnection.InvokeAsync("RegisterAppConfigListener", 
                _options.AppId, _options.Environment);
            
            _logger.LogInformation("已注册应用 {AppId} 在 {Environment} 环境的配置监听", 
                _options.AppId, _options.Environment);
            
            // 开始定期发送心跳
            StartHeartbeat();
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, "连接到配置中心SignalR Hub失败：{Message}", ex.Message);
        }
    }
    
    /// <summary>
    /// 注册客户端信息
    /// </summary>
    private async Task RegisterClientInfoAsync()
    {
        try
        {
            string hostName = Environment.MachineName;
            string version = GetClientVersion();
            
            await _hubConnection.InvokeAsync("RegisterClientInfo", 
                _clientId, 
                _options.AppId, 
                _options.Environment,
                hostName,
                version);
                
            _logger.LogDebug("已向配置中心注册客户端信息");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册客户端信息失败：{Message}", ex.Message);
        }
    }
    
    /// <summary>
    /// 生成客户端唯一标识
    /// </summary>
    private string GenerateClientId()
    {
        try
        {
            // 尝试使用MAC地址作为硬件标识的一部分
            string macAddress = GetMacAddress();
            string processId = Environment.ProcessId.ToString();
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            return $"{_options.AppId}-{macAddress}-{processId}-{timestamp}";
        }
        catch
        {
            // 如果获取MAC地址失败，则使用GUID
            return $"{_options.AppId}-{Guid.NewGuid()}";
        }
    }
    
    /// <summary>
    /// 获取MAC地址
    /// </summary>
    private string GetMacAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => 
                    ni.OperationalStatus == OperationalStatus.Up && 
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                    
            if (networkInterfaces != null)
            {
                string mac = BitConverter.ToString(networkInterfaces.GetPhysicalAddress().GetAddressBytes())
                    .Replace("-", "")
                    .ToLower();
                
                // 只使用部分MAC地址，避免泄露完整硬件信息
                if (mac.Length > 6)
                {
                    return mac.Substring(mac.Length - 6);
                }
                return mac;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取MAC地址失败");
        }
        
        return "nomac";
    }
    
    /// <summary>
    /// 获取客户端版本
    /// </summary>
    private string GetClientVersion()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var version = entryAssembly.GetName().Version;
                return version?.ToString() ?? "未知";
            }
            
            var clientAssembly = Assembly.GetExecutingAssembly();
            if (clientAssembly != null)
            {
                var version = clientAssembly.GetName().Version;
                return version?.ToString() ?? "未知";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取客户端版本失败");
        }
        
        return "未知";
    }
    
    /// <summary>
    /// 开始定期发送心跳
    /// </summary>
    private void StartHeartbeat()
    {
        Task.Run(async () =>
        {
            while (_isConnected)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                    
                    if (_isConnected)
                    {
                        await _hubConnection.InvokeAsync("Heartbeat");
                        _logger.LogTrace("已发送心跳");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "发送心跳失败");
                }
            }
        });
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