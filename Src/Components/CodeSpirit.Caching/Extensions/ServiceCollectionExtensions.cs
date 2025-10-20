using System.Threading;
using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.DistributedLock;
using CodeSpirit.Caching.Models;
using CodeSpirit.Caching.Services;
using CodeSpirit.Core.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CodeSpirit.Caching.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加CodeSpirit缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritCaching(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        return services.AddCodeSpiritCaching(configuration.GetSection(CachingOptions.SectionName), serviceName);
    }

    /// <summary>
    /// 添加CodeSpirit缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">配置节</param>
    /// <param name="serviceName">服务名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritCaching(this IServiceCollection services, IConfigurationSection configurationSection, string serviceName)
    {
        services.Configure<CachingOptions>(options => configurationSection.Bind(options));
        
        // 验证配置
        services.AddSingleton<IValidateOptions<CachingOptions>, CachingOptionsValidator>();

        // 确保内存缓存已注册
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

        // 注册分布式锁提供者（使用默认配置）
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        
        // 配置分布式锁选项（如果配置中没有，使用默认值）
        services.Configure<RedisDistributedLockOptions>(lockOptions =>
        {
            // 使用默认配置
            lockOptions.KeyPrefix = $"CodeSpirit:{serviceName}:";
        });

        // 注册核心服务
        services.TryAddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
        services.TryAddScoped<ICacheService, MultiLevelCacheService>();
        services.TryAddScoped<ICacheWarmupService, CacheWarmupService>();

        return services;
    }

    /// <summary>
    /// 添加CodeSpirit缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritCaching(this IServiceCollection services, Action<CachingOptions>? configureOptions = null)
    {
        // 配置选项
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<CachingOptions>(options => { }); // 使用默认配置
        }

        // 验证配置
        services.AddSingleton<IValidateOptions<CachingOptions>, CachingOptionsValidator>();

        // 注册核心服务
        services.TryAddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();
        services.TryAddScoped<ICacheService, MultiLevelCacheService>();
        services.TryAddScoped<ICacheWarmupService, CacheWarmupService>();

        // 确保内存缓存已注册
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

        // 注册分布式锁提供者（使用默认配置）
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();
        
        // 配置分布式锁选项
        services.Configure<RedisDistributedLockOptions>(lockOptions =>
        {
            // 使用默认配置，如果需要自定义可以通过后续的Configure覆盖
        });

        return services;
    }

    /// <summary>
    /// 添加Redis分布式缓存和锁
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRedisDistributedCacheAndLock(this IServiceCollection services, string connectionString)
    {
        // 添加Redis连接
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var logger = provider.GetService<ILogger<IConnectionMultiplexer>>();
            var configuration = ConfigurationOptions.Parse(connectionString);
            
            // 高并发优化配置
            configuration.AbortOnConnectFail = false;  // 连接失败不中止
            configuration.ConnectTimeout = 10000;      // 10秒连接超时
            configuration.SyncTimeout = 10000;         // 10秒同步超时
            configuration.AsyncTimeout = 10000;        // 10秒异步超时
            configuration.ConnectRetry = 3;            // 重试3次
            configuration.KeepAlive = 60;              // 60秒保活
            configuration.DefaultDatabase = 0;
            
            // 配置线程池设置（考虑分布式环境中的多实例部署）
            // 根据服务层级和实例数量，适度增加最小线程数
            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            var minThreads = Math.Max(workerThreads, 50); // 每个实例最小50个线程（适合3副本场景）
            ThreadPool.SetMinThreads(minThreads, Math.Max(completionPortThreads, 50));
            
            var multiplexer = ConnectionMultiplexer.Connect(configuration);
            
            // 添加连接事件监听
            if (logger != null)
            {
                multiplexer.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis连接失败: {EndPoint}, {FailureType}, {Exception}", 
                        args.EndPoint, args.FailureType, args.Exception?.Message);
                };
                
                multiplexer.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis连接恢复: {EndPoint}", args.EndPoint);
                };
                
                multiplexer.ErrorMessage += (sender, args) =>
                {
                    logger.LogWarning("Redis错误消息: {EndPoint}, {Message}", args.EndPoint, args.Message);
                };
            }
            
            return multiplexer;
        });

        // 添加分布式缓存
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            //options.InstanceName = "CS:"; // 添加实例前缀避免键冲突
        });

        return services;
    }

    /// <summary>
    /// 添加Redis分布式缓存和锁（使用配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="connectionName">连接名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRedisDistributedCacheAndLock(this IServiceCollection services, IConfiguration configuration, string connectionName = "cache")
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"未找到名为 '{connectionName}' 的连接字符串");
        }

        return services.AddRedisDistributedCacheAndLock(connectionString);
    }

    /// <summary>
    /// 添加缓存预热后台服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheWarmupBackgroundService(this IServiceCollection services)
    {
        services.AddHostedService<CacheWarmupBackgroundService>();
        return services;
    }

    /// <summary>
    /// 添加缓存统计服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheStatistics(this IServiceCollection services)
    {
        // TODO: 实现缓存统计服务
        // services.TryAddSingleton<ICacheStatisticsService, CacheStatisticsService>();
        return services;
    }
}

/// <summary>
/// 缓存选项验证器
/// </summary>
internal class CachingOptionsValidator : IValidateOptions<CachingOptions>
{
    /// <summary>
    /// 验证选项
    /// </summary>
    /// <param name="name">选项名称</param>
    /// <param name="options">选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, CachingOptions options)
    {
        if (!options.Validate())
        {
            return ValidateOptionsResult.Fail("缓存配置验证失败：至少需要启用一种缓存，且过期时间必须大于零");
        }

        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// 缓存预热后台服务
/// </summary>
internal class CacheWarmupBackgroundService : BackgroundService
{
    private readonly ICacheWarmupService _warmupService;
    private readonly ILogger<CacheWarmupBackgroundService> _logger;
    private readonly CachingOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="warmupService">缓存预热服务</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="logger">日志记录器</param>
    public CacheWarmupBackgroundService(
        ICacheWarmupService warmupService,
        IOptions<CachingOptions> options,
        ILogger<CacheWarmupBackgroundService> logger)
    {
        _warmupService = warmupService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 执行后台任务
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Warmup.WarmupOnStartup)
        {
            return;
        }

        try
        {
            // 等待启动延迟
            if (_options.Warmup.StartupDelay > TimeSpan.Zero)
            {
                _logger.LogInformation("缓存预热将在 {Delay} 后开始", _options.Warmup.StartupDelay);
                await Task.Delay(_options.Warmup.StartupDelay, stoppingToken);
            }

            _logger.LogInformation("开始启动时缓存预热");

            // TODO: 这里需要从配置或其他地方获取预热项列表
            // 目前只是一个占位符实现
            var warmupItems = GetStartupWarmupItems();
            
            if (warmupItems.Any())
            {
                await _warmupService.WarmupBatchAsync(warmupItems, stoppingToken);
                _logger.LogInformation("启动时缓存预热完成");
            }
            else
            {
                _logger.LogDebug("没有配置启动时预热项");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("缓存预热被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动时缓存预热失败");
        }
    }

    /// <summary>
    /// 获取启动时预热项
    /// </summary>
    /// <returns>预热项列表</returns>
    private List<CacheWarmupItem> GetStartupWarmupItems()
    {
        // TODO: 从配置文件或数据库中读取预热项
        // 这里返回空列表作为占位符
        return new List<CacheWarmupItem>();
    }
}
