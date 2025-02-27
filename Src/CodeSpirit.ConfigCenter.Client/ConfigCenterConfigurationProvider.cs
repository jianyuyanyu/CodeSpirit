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
/// �������������ṩ����
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

        // ����SignalR���ñ���������
        if (_options.UseSignalR)
        {
            _hubClient.OnConfigChanged += OnConfigChangedAsync;
        }

        // ��ʼ����ѯ��ʱ��
        _pollingTimer = new Timer(
            async _ => await PollForChangesAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public override void Load()
    {
        // ע��Ӧ�ã������Ҫ��
        if (_options.AutoRegisterApp)
        {
            try
            {
                var registrationResult = _client.RegisterAppAsync(_cts.Token).GetAwaiter().GetResult();
                if (!registrationResult.Success)
                {
                    _logger.LogWarning("Ӧ���Զ�ע��ʧ�ܣ�{Message}", registrationResult.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ӧ���Զ�ע��ʧ�ܣ�{Message}", ex.Message);
            }
        }

        // ��������
        LoadConfigAsync(_cts.Token).GetAwaiter().GetResult();

        // ���ʹ��SignalR�����ӵ�Hub
        if (_options.UseSignalR)
        {
            _hubClient.ConnectAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "���ӵ���������SignalR Hubʧ��");
                }
            });
        }

        // ������ѯ��ʱ��
        if (_options.PollIntervalSeconds > 0)
        {
            _pollingTimer.Change(
                TimeSpan.FromSeconds(_options.PollIntervalSeconds),
                TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    private async Task LoadConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            // �ж��Ƿ�Ӧ��ʹ�û���
            bool useCache = _options.EnableLocalCache && (
                _options.PreferCache ||
                _initialLoadFailed ||
                !IsNetworkAvailable());

            ConfigItemsExportDto configData = null;

            if (useCache)
            {
                // ���Դӻ����������
                _logger.LogInformation("���Դӱ��ػ����������");
                configData = await _cacheService.LoadFromCacheAsync();

                if (configData != null)
                {
                    _logger.LogInformation("�Ѵӱ��ػ����������");
                }
                else
                {
                    _logger.LogWarning("���ػ��治���û��ѹ���");
                }
            }

            // ������治���û�δ���û��棬��ӷ�������ȡ����
            if (configData == null)
            {
                try
                {
                    _logger.LogInformation("���ڴ��������ķ�������ȡ����");
                    configData = await _client.GetConfigsAsync(cancellationToken);
                    _initialLoadFailed = false;

                    // ��������˻��棬�����ñ��浽����
                    if (_options.EnableLocalCache)
                    {
                        await _cacheService.SaveToCacheAsync(configData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "���������ķ�������ȡ����ʧ�ܣ�{Message}", ex.Message);
                    _initialLoadFailed = true;

                    // ��������˻��棬���ٴγ��Դӻ�����أ���ʹ֮ǰ�����˲�ʹ�û��棩
                    if (_options.EnableLocalCache && !useCache)
                    {
                        _logger.LogInformation("���Դӱ��ػ����������");
                        configData = await _cacheService.LoadFromCacheAsync();

                        if (configData == null)
                        {
                            throw new InvalidOperationException("�޷����������ķ�������ȡ���ã����ػ���Ҳ������", ex);
                        }

                        _logger.LogInformation("�Ѵӱ��ػ���������ã���������ʧ�ܵĻ��˻��ƣ�");
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // ����������ת��Ϊ��ƽ���ļ�ֵ��
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FlattenConfigs(configData.Configs, string.Empty, data);

            // ������������
            Data = data;

            _logger.LogInformation("�Ѽ���Ӧ�� {AppId} �� {Environment} ����������",
                _options.AppId, _options.Environment);

            // �������ñ���¼�
            OnReload();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "��������ʧ�ܣ�{Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// ��������Ƿ����
    /// </summary>
    private bool IsNetworkAvailable()
    {
        try
        {
            // һ�ּ򵥵���������Լ�鷽��
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
    /// ������ת��Ϊ��ƽ���ļ�ֵ��
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
                // �ݹ鴦��Ƕ���ֵ�
                FlattenConfigs(nestedDict, key, data);
            }
            else if (kvp.Value != null)
            {
                // ��Ӽ�ֵ��
                data[key] = kvp.Value.ToString();
            }
        }
    }

    /// <summary>
    /// ���ñ���������
    /// </summary>
    private async Task OnConfigChangedAsync()
    {
        await ReloadConfigAsync();
    }

    /// <summary>
    /// ��ѯ���ñ��
    /// </summary>
    private async Task PollForChangesAsync()
    {
        await ReloadConfigAsync();
    }

    /// <summary>
    /// ���¼�������
    /// </summary>
    private async Task ReloadConfigAsync()
    {
        if (!await _reloadLock.WaitAsync(0))
        {
            return; // ����Ѿ���һ�����¼��ز����ڽ����У�������
        }

        try
        {
            await LoadConfigAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "���¼�������ʧ�ܣ�{Message}", ex.Message);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <summary>
    /// �ͷ���Դ
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