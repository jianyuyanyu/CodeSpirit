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
    private readonly ConcurrentDictionary<string, string> _configVersions = new();
    private bool _initialLoadFailed = false;

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
        //// 将应用注册逻辑与主要配置加载流程分离
        //if (_options.AutoRegisterApp)
        //{
        //    try
        //    {
        //        var registrationResult = _client.RegisterAppAsync(_cts.Token).GetAwaiter().GetResult();
        //        if (registrationResult.Success)
        //        {
        //            _logger.LogInformation("应用 {AppId} 自动注册成功", _options.AppId);
        //            // 如果配置中没有设置 AppSecret，但注册成功时获取了新的密钥，则更新它
        //            if (string.IsNullOrEmpty(_options.AppSecret) && !string.IsNullOrEmpty(registrationResult.Secret))
        //            {
        //                _logger.LogInformation("已获取新的应用密钥");
        //                // 在这里不能直接修改 _options 中的值，因为它来自 IOptions<T>，是只读的
        //                // 可以考虑使用其他方式存储获取到的密钥，如：
        //                _client.UpdateAppSecret(registrationResult.Secret);
        //            }
        //        }
        //        else
        //        {
        //            _logger.LogWarning("应用自动注册失败：{Message}", registrationResult.Message);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "应用自动注册失败：{Message}", ex.Message);
        //        // 注册失败不影响其他服务
        //    }
        //}

        // 加载配置（即使应用注册失败也会执行）
        LoadConfigAsync(_cts.Token).GetAwaiter().GetResult();

        // 启动 SignalR 连接（与应用注册无关）
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

        // 启动轮询定时器（与应用注册无关）
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
            // 判断是否应该使用缓存
            bool useCache = _options.EnableLocalCache && (
                _options.PreferCache ||
                _initialLoadFailed ||
                !IsNetworkAvailable());

            ConfigItemsExportDto configData = null;

            if (useCache)
            {
                // 尝试从缓存加载配置
                _logger.LogInformation("尝试从本地缓存加载配置");
                configData = await _cacheService.LoadFromCacheAsync();

                if (configData != null)
                {
                    _logger.LogInformation("已从本地缓存加载配置");
                }
                else
                {
                    _logger.LogWarning("本地缓存不可用或已过期");
                }
            }

            // 如果缓存不可用或未启用缓存，则从服务器获取配置
            if (configData == null)
            {
                try
                {
                    _logger.LogInformation("正在从配置中心服务器获取配置");
                    configData = await _client.GetConfigsAsync(cancellationToken);
                    _initialLoadFailed = false;

                    // 如果启用了缓存，则将配置保存到缓存
                    if (_options.EnableLocalCache)
                    {
                        await _cacheService.SaveToCacheAsync(configData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从配置中心服务器获取配置失败：{Message}", ex.Message);
                    _initialLoadFailed = true;

                    // 如果启用了缓存，则再次尝试从缓存加载（即使之前设置了不使用缓存）
                    if (_options.EnableLocalCache && !useCache)
                    {
                        _logger.LogInformation("尝试从本地缓存加载配置");
                        configData = await _cacheService.LoadFromCacheAsync();

                        if (configData == null)
                        {
                            _logger.LogError(ex, "无法从配置中心服务器获取配置，本地缓存也不可用");
                            return;
                        }
                        else
                        {
                            _logger.LogInformation("已从本地缓存加载配置（网络请求失败的回退机制）");
                        }
                    }
                }
            }

            // 将配置数据转换为扁平化的键值对
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FlattenConfigs(configData.Configs, string.Empty, data);

            // 更新配置数据
            Data = data;

            _logger.LogInformation("已加载应用 {AppId} 在 {Environment} 环境的配置",
                _options.AppId, _options.Environment);

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
    /// 检查网络是否可用
    /// </summary>
    private bool IsNetworkAvailable()
    {
        try
        {
            // 一种简单的网络可用性检查方法
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send("8.8.8.8", 1000);
            return reply?.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch
        {
            return false;
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