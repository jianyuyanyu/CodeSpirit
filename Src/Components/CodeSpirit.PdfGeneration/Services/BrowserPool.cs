using CodeSpirit.PdfGeneration.Exceptions;
using CodeSpirit.PdfGeneration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using PuppeteerSharp;

namespace CodeSpirit.PdfGeneration.Services;

/// <summary>
/// 浏览器实例池，用于管理和复用浏览器实例
/// </summary>
public class BrowserPool : IAsyncDisposable
{
    private readonly ObjectPool<IBrowser> _pool;
    private readonly SemaphoreSlim _semaphore;
    private readonly PdfGenerationOptions _options;
    private readonly ILogger<BrowserPool> _logger;
    private bool _isInitialized = false;
    private bool _disposed = false;

    /// <summary>
    /// 创建浏览器池实例
    /// </summary>
    /// <param name="options">PDF生成选项</param>
    /// <param name="logger">日志记录器</param>
    public BrowserPool(PdfGenerationOptions options, ILogger<BrowserPool> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _semaphore = new SemaphoreSlim(options.MaxConcurrentJobs, options.MaxConcurrentJobs);
        
        // 创建浏览器池
        var poolPolicy = new BrowserPoolPolicy(options, logger);
        _pool = new DefaultObjectPool<IBrowser>(poolPolicy, options.BrowserPoolSize);
    }

    /// <summary>
    /// 初始化浏览器池
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _logger.LogInformation("正在初始化浏览器池...");
        
        // 下载浏览器（如果需要）
        if (string.IsNullOrEmpty(_options.ExecutablePath))
        {
            _logger.LogInformation("未指定浏览器路径，正在下载浏览器...");
            
            // 使用 BrowserFetcher 下载浏览器，带重试机制
            var retryCount = 0;
            var maxRetries = 3;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    var browserFetcher = new BrowserFetcher();
                    
                    // 确保浏览器已下载 (PuppeteerSharp 13.x 会自动检查是否已下载)
                    _logger.LogInformation("正在确保 Chromium 浏览器已下载...");
                    await browserFetcher.DownloadAsync();
                    _logger.LogInformation("Chromium 浏览器准备就绪");
                    
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "下载浏览器失败 (尝试 {RetryCount}/{MaxRetries})", retryCount, maxRetries);
                    
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError("下载浏览器失败，已达到最大重试次数");
                        throw;
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }
        
        // 预热浏览器池
        var initTasks = new List<Task>();
        for (int i = 0; i < _options.BrowserPoolSize; i++)
        {
            initTasks.Add(Task.Run(async () =>
            {
                try
                {
                    _logger.LogDebug("正在预热浏览器实例 {Index}...", i + 1);
                    var browser = _pool.Get();
                    _pool.Return(browser);
                    _logger.LogDebug("浏览器实例 {Index} 预热完成", i + 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "预热浏览器实例 {Index} 失败", i + 1);
                    throw;
                }
                await Task.CompletedTask;
            }));
        }
        
        await Task.WhenAll(initTasks);
        _isInitialized = true;
        _logger.LogInformation("浏览器池初始化完成，大小: {PoolSize}", _options.BrowserPoolSize);
    }

    /// <summary>
    /// 获取浏览器实例
    /// </summary>
    public async Task<IBrowser> GetBrowserAsync()
    {
        if (!_isInitialized)
            throw new PdfGenerationNotInitializedException();
        
        await _semaphore.WaitAsync();
        try
        {
            return _pool.Get();
        }
        catch (Exception ex)
        {
            _semaphore.Release();
            _logger.LogError(ex, "获取浏览器实例失败");
            throw new BrowserAcquisitionException("获取浏览器实例失败", ex);
        }
    }

    /// <summary>
    /// 归还浏览器实例到池中
    /// </summary>
    public void ReturnBrowser(IBrowser browser)
    {
        if (browser == null)
            throw new ArgumentNullException(nameof(browser));
        
        try
        {
            _pool.Return(browser);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        // 释放所有浏览器实例
        for (int i = 0; i < _options.BrowserPoolSize; i++)
        {
            try
            {
                var browser = _pool.Get();
                await browser.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放浏览器实例失败");
            }
        }
        
        _semaphore.Dispose();
        _logger.LogInformation("浏览器池已释放");
    }

    /// <summary>
    /// 浏览器池策略，负责创建和回收浏览器实例
    /// </summary>
    private class BrowserPoolPolicy : IPooledObjectPolicy<IBrowser>
    {
        private readonly PdfGenerationOptions _options;
        private readonly ILogger _logger;

        public BrowserPoolPolicy(PdfGenerationOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
        }

        public IBrowser Create()
        {
            try
            {
                _logger.LogDebug("创建新的浏览器实例");
                
                var launchOptions = new LaunchOptions
                {
                    Headless = _options.Headless,
                    Args = _options.BrowserArguments,
                    Timeout = (int)_options.BrowserTimeout.TotalMilliseconds
                };
                
                // 如果指定了浏览器路径，则使用指定的路径
                if (!string.IsNullOrEmpty(_options.ExecutablePath))
                {
                    launchOptions.ExecutablePath = _options.ExecutablePath;
                }
                
                // 构建启动参数列表
                var args = launchOptions.Args?.ToList() ?? new List<string>();
                
                // 如果指定了内存限制，则设置内存限制
                if (_options.BrowserMemoryLimit.HasValue)
                {
                    args.Add($"--js-flags=--max-old-space-size={_options.BrowserMemoryLimit}");
                }
                
                // 在 Windows 环境下添加用户数据目录，避免权限问题
                if (OperatingSystem.IsWindows())
                {
                    var userDataDir = Path.Combine(Path.GetTempPath(), "puppeteer_dev_chrome_profile");
                    if (!Directory.Exists(userDataDir))
                    {
                        Directory.CreateDirectory(userDataDir);
                        _logger.LogInformation("创建用户数据目录: {UserDataDir}", userDataDir);
                    }
                    args.Add($"--user-data-dir={userDataDir}");
                }
                
                launchOptions.Args = args.ToArray();
                
                _logger.LogDebug("浏览器启动参数: {Args}", string.Join(" ", launchOptions.Args));
                
                return Puppeteer.LaunchAsync(launchOptions).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建浏览器实例失败");
                throw new BrowserAcquisitionException("创建浏览器实例失败", ex);
            }
        }

        public bool Return(IBrowser obj)
        {
            // 检查浏览器是否仍然可用
            if (obj.IsClosed)
            {
                _logger.LogWarning("浏览器实例已关闭，将创建新实例");
                return false;
            }
            
            // 清理未关闭的页面
            try
            {
                var pages = obj.PagesAsync().GetAwaiter().GetResult();
                foreach (var page in pages)
                {
                    if (!page.IsClosed)
                    {
                        page.CloseAsync().GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理浏览器页面失败");
                return false;
            }
            
            return true;
        }
    }
}