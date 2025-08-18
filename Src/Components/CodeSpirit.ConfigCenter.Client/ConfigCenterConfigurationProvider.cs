using System.Collections.Concurrent;
using CodeSpirit.ConfigCenter.Client.Cache;
using CodeSpirit.ConfigCenter.Client.Models;
using CodeSpirit.ConfigCenter.Client.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// 配置中心配置提供程序
/// </summary>
public class ConfigCenterConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly ConfigCenterClient _client;
    private readonly ConfigCenterClientOptions _options;
    private readonly ILogger<ConfigCenterConfigurationProvider> _logger;
    private readonly ConfigCenterHubClient _hubClient;
    private readonly ConfigCacheService _cacheService;
    private readonly CancellationTokenSource _cts = new();
    private readonly Timer _pollingTimer;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public ConfigCenterConfigurationProvider(
        ConfigCenterClient client,
        ConfigCenterHubClient hubClient,
        ConfigCacheService cacheService,
        IOptions<ConfigCenterClientOptions> options,
        ILogger<ConfigCenterConfigurationProvider> logger)
    {
        _client = client;
        _hubClient = hubClient;
        _cacheService = cacheService;
        _options = options.Value;
        _logger = logger;

        // 设置SignalR配置变更处理程序
        if (_options.UseSignalR)
        {
            _hubClient.OnConfigChanged += OnConfigChangedAsync;
        }

        // 初始化轮询定时器
        _pollingTimer = new Timer(
            async _ => await PollForChangesAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    public override void Load()
    {
        // 加载配置
        LoadConfigAsync(_cts.Token).GetAwaiter().GetResult();

        // 启动 SignalR 连接
        if (_options.UseSignalR)
        {
            _hubClient.ConnectAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "连接到配置中心SignalR Hub失败");
                }
            });
        }

        // 启动轮询定时器
        if (_options.PollIntervalSeconds > 0)
        {
            _pollingTimer.Change(
                TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private async Task LoadConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            ConfigItemsExportDto configData = null;
            bool loadedFromCache = false;

            // 尝试从缓存加载
            if (_options.EnableLocalCache)
            {
                _logger.LogDebug("尝试从本地缓存加载配置");
                configData = await _cacheService.LoadFromCacheAsync();
                if (configData != null)
                {
                    loadedFromCache = true;
                    _logger.LogInformation("已从本地缓存加载配置");
                }
            }

            // 如果缓存不可用或未启用缓存，则从服务器获取配置
            if (configData == null && !_options.PreferCache)
            {
                try
                {
                    _logger.LogInformation("正在从配置中心服务器获取配置");
                    configData = await _client.GetConfigsAsync(cancellationToken);

                    // 如果启用了缓存，则将配置保存到缓存
                    if (_options.EnableLocalCache && configData != null)
                    {
                        await _cacheService.SaveToCacheAsync(configData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从配置中心服务器获取配置失败：{Message}", ex.Message);
                    
                    // 如果已经有缓存数据则使用
                    if (configData != null)
                    {
                        _logger.LogWarning("使用缓存数据作为回退方案");
                    }
                    else
                    {
                        throw; // 没有缓存数据且请求失败，向上抛出异常
                    }
                }
            }

            if (configData == null)
            {
                _logger.LogWarning("无法获取配置数据");
                return;
            }

            // 将配置数据转换为扁平化的键值对
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FlattenConfigs(configData.Configs, string.Empty, data);

            // 更新配置数据
            Data = data;

            _logger.LogInformation("已加载应用 {AppId} 在 {Environment} 环境的配置{Source}",
                _options.AppId, _options.Environment, loadedFromCache ? " (来自缓存)" : "");

            // 触发配置变更事件
            OnReload();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置失败：{Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 将配置转换为扁平化的键值对
    /// </summary>
    private void FlattenConfigs(
        Dictionary<string, object> configs,
        string prefix,
        Dictionary<string, string> data)
    {
        if (configs == null) return;
        
        foreach (var kvp in configs)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                // 递归处理嵌套字典
                FlattenConfigs(nestedDict, key, data);
            }
            else if (kvp.Value != null)
            {
                // 添加键值对
                data[key] = kvp.Value.ToString();
            }
        }
    }

    /// <summary>
    /// 配置变更处理程序
    /// </summary>
    private async Task OnConfigChangedAsync()
    {
        await ReloadConfigAsync();
    }

    /// <summary>
    /// 轮询配置变更
    /// </summary>
    private async Task PollForChangesAsync()
    {
        await ReloadConfigAsync();
    }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    private async Task ReloadConfigAsync()
    {
        if (!await _reloadLock.WaitAsync(0))
        {
            return; // 如果已经有一个重新加载操作在进行中，则跳过
        }

        try
        {
            await LoadConfigAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载配置失败：{Message}", ex.Message);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _pollingTimer?.Dispose();
        _reloadLock?.Dispose();
        (_hubClient as IAsyncDisposable)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _cts.Dispose();
    }
}