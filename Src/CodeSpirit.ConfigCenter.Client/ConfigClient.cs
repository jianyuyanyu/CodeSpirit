using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Client
{
    //public class ConfigClient : IConfigClient
    //{
    //    private readonly HttpClient _httpClient;
    //    private readonly HubConnection _hubConnection;
    //    private readonly ILogger<ConfigClient> _logger;

    //    public ConfigClient(
    //        HttpClient httpClient,
    //        ILogger<ConfigClient> logger,
    //        string hubUrl)
    //    {
    //        _httpClient = httpClient;
    //        _logger = logger;
            
    //        _hubConnection = new HubConnectionBuilder()
    //            .WithUrl(hubUrl)
    //            .WithAutomaticReconnect()
    //            .Build();

    //        _hubConnection.On<ConfigChangeEvent>("ConfigChanged", OnConfigChanged);
    //    }

    //    public async Task StartAsync()
    //    {
    //        await _hubConnection.StartAsync();
    //        _logger.LogInformation("Connected to config hub");
    //    }

    //    private void OnConfigChanged(ConfigChangeEvent change)
    //    {
    //        _logger.LogInformation(
    //            "Configuration changed. Group: {Group}, Key: {Key}",
    //            change.Group, change.Key);
                
    //        // 触发配置变更事件
    //        ConfigChanged?.Invoke(this, change);
    //    }

    //    public event EventHandler<ConfigChangeEvent> ConfigChanged;
    //}
} 