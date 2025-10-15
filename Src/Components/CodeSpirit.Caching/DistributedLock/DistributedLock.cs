namespace CodeSpirit.Caching.DistributedLock;

/// <summary>
/// 分布式锁实现类
/// </summary>
public class DistributedLock : IDisposable, IAsyncDisposable
{
    private readonly IDistributedLockProvider _provider;
    private readonly string _key;
    private readonly string _lockValue;
    private readonly CancellationTokenSource _watchdogCancellation;
    private readonly Task? _watchdogTask;
    private bool _disposed;

    /// <summary>
    /// 锁的键名
    /// </summary>
    public string Key => _key;

    /// <summary>
    /// 锁的值
    /// </summary>
    public string LockValue => _lockValue;

    /// <summary>
    /// 锁的获取时间
    /// </summary>
    public DateTime AcquiredTime { get; }

    /// <summary>
    /// 是否已释放
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="provider">分布式锁提供程序</param>
    /// <param name="key">锁的键名</param>
    /// <param name="lockValue">锁的值</param>
    /// <param name="enableWatchdog">是否启用看门狗</param>
    /// <param name="watchdogInterval">看门狗间隔</param>
    /// <param name="lockTtl">锁的生存时间</param>
    public DistributedLock(
        IDistributedLockProvider provider, 
        string key, 
        string lockValue,
        bool enableWatchdog = false,
        TimeSpan? watchdogInterval = null,
        TimeSpan? lockTtl = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _lockValue = lockValue ?? throw new ArgumentNullException(nameof(lockValue));
        AcquiredTime = DateTime.UtcNow;
        
        if (enableWatchdog && watchdogInterval.HasValue && lockTtl.HasValue)
        {
            _watchdogCancellation = new CancellationTokenSource();
            _watchdogTask = StartWatchdog(watchdogInterval.Value, lockTtl.Value, _watchdogCancellation.Token);
        }
        else
        {
            _watchdogCancellation = new CancellationTokenSource();
        }
    }

    /// <summary>
    /// 启动看门狗任务
    /// </summary>
    /// <param name="interval">检查间隔</param>
    /// <param name="lockTtl">锁的生存时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>看门狗任务</returns>
    private async Task StartWatchdog(TimeSpan interval, TimeSpan lockTtl, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested)
                {
                    await _provider.ExtendLockAsync(_key, lockTtl);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
        catch (Exception)
        {
            // 看门狗失败，但不影响主流程
        }
    }

    /// <summary>
    /// 释放锁资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 异步释放锁资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放锁资源
    /// </summary>
    /// <param name="disposing">是否为显式释放</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _watchdogCancellation?.Cancel();
                _watchdogTask?.Wait(TimeSpan.FromSeconds(1));
                _provider.ReleaseLockAsync(_key).GetAwaiter().GetResult();
                _watchdogCancellation?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 异步释放核心资源
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _watchdogCancellation?.Cancel();
            
            if (_watchdogTask != null)
            {
                try
                {
                    await _watchdogTask.WaitAsync(TimeSpan.FromSeconds(1));
                }
                catch (TimeoutException)
                {
                    // 超时忽略
                }
            }
            
            await _provider.ReleaseLockAsync(_key);
            _watchdogCancellation?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~DistributedLock()
    {
        Dispose(false);
    }
}
