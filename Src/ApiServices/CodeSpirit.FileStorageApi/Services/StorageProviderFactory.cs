using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Options;
using CodeSpirit.FileStorageApi.Providers;
using CodeSpirit.Core;
using Microsoft.Extensions.Options;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 存储提供程序工厂实现
/// </summary>
public class StorageProviderFactory : IStorageProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IStorageProvider> _providers;
    private readonly ILogger<StorageProviderFactory> _logger;
    private readonly FileStorageOptions _fileStorageOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<StorageProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _fileStorageOptions = fileStorageOptions?.Value ?? throw new ArgumentNullException(nameof(fileStorageOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = new Dictionary<string, IStorageProvider>();
    }

    /// <summary>
    /// 根据提供程序名称获取存储提供程序实例
    /// </summary>
    public IStorageProvider GetProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrEmpty(providerName);

        // 首先检查已缓存的实例
        if (_providers.TryGetValue(providerName, out var cachedProvider))
        {
            return cachedProvider;
        }

        // 从配置中查找提供程序配置
        if (!_fileStorageOptions.StorageProviders.TryGetValue(providerName, out var providerOptions))
        {
            throw new AppServiceException(500, $"未找到存储提供程序配置: {providerName}");
        }

        // 根据类型创建提供程序实例
        IStorageProvider provider = providerOptions.Type switch
        {
            StorageProviderType.Local => new LocalStorageProvider(
                providerOptions,
                _serviceProvider.GetRequiredService<ILogger<LocalStorageProvider>>()),
            
            StorageProviderType.TencentCOS => new TencentCosStorageProvider(
                _serviceProvider.GetRequiredService<IOptions<TencentCosOptions>>(),
                _serviceProvider.GetRequiredService<ILogger<TencentCosStorageProvider>>()),
            
            _ => throw new AppServiceException(500, $"不支持的存储提供程序类型: {providerOptions.Type}")
        };

        _providers[providerName] = provider; // 缓存实例
        return provider;
    }

    /// <summary>
    /// 根据提供程序类型获取存储提供程序实例
    /// </summary>
    public IStorageProvider GetProvider(StorageProviderType providerType)
    {
        var providerName = providerType.ToString();
        return GetProvider(providerName);
    }

    /// <summary>
    /// 获取所有可用的存储提供程序
    /// </summary>
    public IEnumerable<IStorageProvider> GetAllProviders()
    {
        var providers = new List<IStorageProvider>();
        
        foreach (var providerName in _fileStorageOptions.StorageProviders.Keys)
        {
            try
            {
                providers.Add(GetProvider(providerName));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法创建存储提供程序: {ProviderName}", providerName);
            }
        }
        
        return providers;
    }

    /// <summary>
    /// 注册存储提供程序
    /// </summary>
    public void RegisterProvider(string name, IStorageProvider provider)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(provider);

        _providers[name] = provider;
        _logger.LogInformation("已注册存储提供程序: {ProviderName}", name);
    }
}
