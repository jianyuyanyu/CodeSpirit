using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.Core;

namespace CodeSpirit.FileStorageApi.Services;

/// <summary>
/// 存储提供程序工厂实现
/// </summary>
public class StorageProviderFactory : IStorageProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _providerTypes;
    private readonly Dictionary<string, IStorageProvider> _providers;
    private readonly ILogger<StorageProviderFactory> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<StorageProviderFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerTypes = new Dictionary<string, Type>();
        _providers = new Dictionary<string, IStorageProvider>();
        
        InitializeDefaultProviders();
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

        // 尝试从注册的类型创建实例
        if (_providerTypes.TryGetValue(providerName, out var providerType))
        {
            var provider = (IStorageProvider)ActivatorUtilities.CreateInstance(_serviceProvider, providerType);
            _providers[providerName] = provider; // 缓存实例
            return provider;
        }

        throw new AppServiceException(500, $"未找到存储提供程序: {providerName}");
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
        
        foreach (var providerName in _providerTypes.Keys)
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

    /// <summary>
    /// 注册存储提供程序类型
    /// </summary>
    public void RegisterProviderType<T>(string name) where T : class, IStorageProvider
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        
        _providerTypes[name] = typeof(T);
        _logger.LogInformation("已注册存储提供程序类型: {ProviderName} -> {ProviderType}", name, typeof(T).Name);
    }

    /// <summary>
    /// 初始化默认提供程序
    /// </summary>
    private void InitializeDefaultProviders()
    {
        try
        {
            // 注册本地存储提供程序
            RegisterProviderType<Providers.LocalStorageProvider>("Local");
            
            // 注册腾讯云COS提供程序
            RegisterProviderType<Providers.TencentCosStorageProvider>("TencentCOS");
            
            _logger.LogInformation("默认存储提供程序初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化默认存储提供程序失败");
        }
    }
}
