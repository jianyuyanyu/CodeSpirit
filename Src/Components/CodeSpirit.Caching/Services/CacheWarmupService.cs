using CodeSpirit.Caching.Abstractions;
using CodeSpirit.Caching.Configuration;
using CodeSpirit.Caching.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CodeSpirit.Caching.Services;

/// <summary>
/// 缓存预热服务实现
/// </summary>
public class CacheWarmupService : ICacheWarmupService
{
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly CachingOptions _options;
    
    // 预热状态跟踪
    private readonly ConcurrentDictionary<string, CacheWarmupStatus> _warmupStatus = new();
    
    // 预热任务取消令牌
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _warmupCancellations = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="keyGenerator">缓存键生成器</param>
    /// <param name="options">缓存配置选项</param>
    /// <param name="logger">日志记录器</param>
    public CacheWarmupService(
        ICacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        IOptions<CachingOptions> options,
        ILogger<CacheWarmupService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 异步预热单个缓存项
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">值工厂方法</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WarmupAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        var fullKey = _keyGenerator.GenerateKey("data", key);
        var statusKey = _keyGenerator.GenerateKey("warmup", key);

        // 创建预热状态
        var status = new CacheWarmupStatus
        {
            Key = fullKey,
            State = WarmupState.InProgress,
            StartTime = DateTime.UtcNow,
            ProgressPercentage = 0
        };

        _warmupStatus.AddOrUpdate(statusKey, status, (k, v) => status);

        try
        {
            _logger.LogInformation("开始预热缓存: {Key}", fullKey);

            // 检查缓存是否已存在（使用原始key，让ExistsAsync内部生成fullKey）
            var exists = await _cacheService.ExistsAsync(key, cancellationToken);
            if (exists)
            {
                _logger.LogDebug("缓存已存在，跳过预热: {Key}", fullKey);
                status.State = WarmupState.Completed;
                status.CompletedTime = DateTime.UtcNow;
                status.ProgressPercentage = 100;
                return;
            }

            status.ProgressPercentage = 25;

            // 调用工厂方法获取值
            var value = await factory();
            status.ProgressPercentage = 75;

            // 设置缓存（使用原始key，让SetAsync内部再次生成fullKey）
            await _cacheService.SetAsync(key, value, options, cancellationToken);
            status.ProgressPercentage = 100;

            // 更新状态
            status.State = WarmupState.Completed;
            status.CompletedTime = DateTime.UtcNow;

            _logger.LogInformation("缓存预热完成: {Key}, 耗时: {Duration}ms", 
                fullKey, status.Duration?.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            status.State = WarmupState.Cancelled;
            status.CompletedTime = DateTime.UtcNow;
            _logger.LogWarning("缓存预热被取消: {Key}", fullKey);
        }
        catch (TimeoutException ex)
        {
            status.State = WarmupState.Timeout;
            status.CompletedTime = DateTime.UtcNow;
            status.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "缓存预热超时: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            status.State = WarmupState.Failed;
            status.CompletedTime = DateTime.UtcNow;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "缓存预热失败: {Key}", fullKey);
        }
        finally
        {
            // 清理取消令牌
            _warmupCancellations.TryRemove(statusKey, out var cts);
            cts?.Dispose();
        }
    }

    /// <summary>
    /// 异步批量预热缓存项
    /// </summary>
    /// <param name="items">预热项集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WarmupBatchAsync(IEnumerable<CacheWarmupItem> items, CancellationToken cancellationToken = default)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            _logger.LogDebug("没有需要预热的缓存项");
            return;
        }

        _logger.LogInformation("开始批量预热缓存，共 {Count} 项", itemList.Count);

        // 按优先级排序
        var sortedItems = itemList.OrderByDescending(x => x.Priority).ToList();

        // 分组：并行和串行
        var parallelItems = sortedItems.Where(x => x.Parallel).ToList();
        var serialItems = sortedItems.Where(x => !x.Parallel).ToList();

        var allTasks = new List<Task>();

        // 处理并行项
        if (parallelItems.Count > 0)
        {
            var parallelTasks = parallelItems.Select(item => WarmupItemAsync(item, cancellationToken));
            
            // 使用信号量限制并发度
            var semaphore = new SemaphoreSlim(_options.Warmup.Concurrency, _options.Warmup.Concurrency);
            var limitedTasks = parallelTasks.Select(async task =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    await task;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            allTasks.AddRange(limitedTasks);
        }

        // 处理串行项
        if (serialItems.Count > 0)
        {
            var serialTask = Task.Run(async () =>
            {
                foreach (var item in serialItems)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await WarmupItemAsync(item, cancellationToken);
                }
            }, cancellationToken);

            allTasks.Add(serialTask);
        }

        // 等待所有任务完成
        try
        {
            await Task.WhenAll(allTasks);
            _logger.LogInformation("批量预热缓存完成，共 {Count} 项", itemList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量预热缓存时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 异步预热缓存项（带参数）
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <typeparam name="TParam">参数类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">带参数的值工厂方法</param>
    /// <param name="parameter">工厂方法参数</param>
    /// <param name="options">缓存选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task WarmupAsync<T, TParam>(string key, Func<TParam, Task<T>> factory, TParam parameter, CacheOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        // 将带参数的工厂方法转换为无参数的
        Func<Task<T>> parameterlessFactory = () => factory(parameter);
        
        await WarmupAsync(key, parameterlessFactory, options, cancellationToken);
    }

    /// <summary>
    /// 异步检查预热状态
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热状态信息</returns>
    public async Task<CacheWarmupStatus> GetWarmupStatusAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var statusKey = _keyGenerator.GenerateKey("warmup", key);
        
        if (_warmupStatus.TryGetValue(statusKey, out var status))
        {
            return status;
        }

        // 如果没有预热状态，检查缓存是否存在
        var fullKey = _keyGenerator.GenerateKey("data", key);
        var exists = await _cacheService.ExistsAsync(key, cancellationToken);
        
        return new CacheWarmupStatus
        {
            Key = fullKey,
            State = exists ? WarmupState.Completed : WarmupState.NotStarted,
            ProgressPercentage = exists ? 100 : 0
        };
    }

    /// <summary>
    /// 取消预热任务
    /// </summary>
    /// <param name="key">缓存键</param>
    public void CancelWarmup(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var statusKey = _keyGenerator.GenerateKey("warmup", key);
        
        if (_warmupCancellations.TryGetValue(statusKey, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("已取消预热任务: {Key}", key);
        }
    }

    /// <summary>
    /// 清理过期的预热状态
    /// </summary>
    /// <param name="maxAge">最大保留时间</param>
    public void CleanupExpiredStatus(TimeSpan? maxAge = null)
    {
        maxAge ??= TimeSpan.FromHours(1);
        var cutoffTime = DateTime.UtcNow - maxAge.Value;

        var expiredKeys = _warmupStatus
            .Where(kvp => kvp.Value.CompletedTime.HasValue && kvp.Value.CompletedTime < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _warmupStatus.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("清理了 {Count} 个过期的预热状态", expiredKeys.Count);
        }
    }

    /// <summary>
    /// 预热单个项目
    /// </summary>
    /// <param name="item">预热项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热任务</returns>
    private async Task WarmupItemAsync(CacheWarmupItem item, CancellationToken cancellationToken)
    {
        if (item.Factory == null)
        {
            _logger.LogWarning("预热项缺少工厂方法: {Key}", item.Key);
            return;
        }

        var statusKey = _keyGenerator.GenerateKey("warmup", item.Key);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _warmupCancellations.TryAdd(statusKey, cts);

        // 设置超时
        if (item.Timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(item.Timeout);
        }

        var retryCount = 0;
        var maxRetries = Math.Max(0, item.RetryCount);

        while (retryCount <= maxRetries)
        {
            try
            {
                // 创建泛型工厂方法
                Func<Task<object>> factory = item.Factory;
                
                await WarmupAsync(item.Key, factory, item.Options, cts.Token);
                return; // 成功，退出重试循环
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                // 被取消，不重试
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;
                
                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex, "预热项失败，已达到最大重试次数: {Key}, 重试次数: {RetryCount}", 
                        item.Key, retryCount - 1);
                    throw;
                }

                _logger.LogWarning(ex, "预热项失败，将在 {Delay}ms 后重试: {Key}, 当前重试次数: {RetryCount}/{MaxRetries}", 
                    item.RetryInterval.TotalMilliseconds, item.Key, retryCount, maxRetries);

                await Task.Delay(item.RetryInterval, cts.Token);
            }
        }
    }
}
