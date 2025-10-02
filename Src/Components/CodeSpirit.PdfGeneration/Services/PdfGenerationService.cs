using System.Collections.Concurrent;
using System.Diagnostics;
using CodeSpirit.PdfGeneration.Exceptions;
using CodeSpirit.PdfGeneration.Options;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace CodeSpirit.PdfGeneration.Services;

/// <summary>
/// PDF生成服务实现
/// </summary>
public class PdfGenerationService : IPdfGenerationService
{
    private readonly BrowserPool _browserPool;
    private readonly PdfGenerationOptions _options;
    private readonly ILogger<PdfGenerationService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly Stopwatch _uptime = new();
    private readonly ConcurrentDictionary<string, int> _activeTasks = new();
    private long _totalTasksProcessed;
    private long _failedTasks;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// 创建PDF生成服务实例
    /// </summary>
    /// <param name="options">PDF生成选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="browserPoolLogger">浏览器池日志记录器</param>
    public PdfGenerationService(
        PdfGenerationOptions options,
        ILogger<PdfGenerationService> logger,
        ILogger<BrowserPool> browserPoolLogger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (browserPoolLogger == null) throw new ArgumentNullException(nameof(browserPoolLogger));
        _browserPool = new BrowserPool(options, browserPoolLogger);
        
        // 创建重试策略
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _options.RetryCount,
                retryAttempt => _options.RetryDelay + TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, 
                        "生成PDF时发生错误，将在 {RetryDelay} 后进行第 {RetryCount}/{MaxRetries} 次重试",
                        timeSpan, retryCount, _options.RetryCount);
                });
        
        _uptime.Start();
    }

    /// <summary>
    /// 初始化服务
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        
        _logger.LogInformation("正在初始化PDF生成服务...");
        await _browserPool.InitializeAsync();
        _isInitialized = true;
        _logger.LogInformation("PDF生成服务初始化完成");
    }

    /// <summary>
    /// 从HTML内容生成PDF
    /// </summary>
    public async Task<byte[]> GeneratePdfAsync(string htmlContent, PdfOptions? options = null)
    {
        EnsureInitialized();
        
        var taskId = Guid.NewGuid().ToString("N");
        _activeTasks[taskId] = 1;
        
        try
        {
            _logger.LogDebug("开始生成PDF，任务ID: {TaskId}", taskId);
            
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var browser = await _browserPool.GetBrowserAsync();
                try
                {
                    var page = await browser.NewPageAsync();
                    try
                    {
                        // 设置页面内容
                        await page.SetContentAsync(htmlContent, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                            Timeout = (int)_options.BrowserTimeout.TotalMilliseconds
                        });
                        
                        // 应用默认PDF选项
                        var pdfOptions = options ?? new PdfOptions
                        {
                            Format = PaperFormat.A4,
                            PrintBackground = true,
                            MarginOptions = new MarginOptions
                            {
                                Top = "10mm",
                                Bottom = "10mm",
                                Left = "10mm",
                                Right = "10mm"
                            }
                        };
                        
                        // 生成PDF
                        var pdfData = await page.PdfDataAsync(pdfOptions);
                        _logger.LogDebug("PDF生成成功，任务ID: {TaskId}, 大小: {Size} 字节", taskId, pdfData.Length);
                        
                        Interlocked.Increment(ref _totalTasksProcessed);
                        return pdfData;
                    }
                    finally
                    {
                        // 关闭页面
                        if (!page.IsClosed)
                            await page.CloseAsync();
                    }
                }
                finally
                {
                    // 归还浏览器实例
                    _browserPool.ReturnBrowser(browser);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成PDF失败，任务ID: {TaskId}", taskId);
            Interlocked.Increment(ref _failedTasks);
            throw new PdfGenerationException(PdfGenerationErrorCodes.GenerationFailed, "生成PDF失败", ex);
        }
        finally
        {
            _activeTasks.TryRemove(taskId, out _);
        }
    }

    /// <summary>
    /// 从HTML内容批量生成PDF
    /// </summary>
    public async Task<IList<byte[]>> GeneratePdfBatchAsync(IEnumerable<string> htmlContents, PdfOptions? options = null)
    {
        EnsureInitialized();
        
        var htmlList = htmlContents.ToList();
        if (!htmlList.Any())
            return new List<byte[]>();
        
        _logger.LogInformation("开始批量生成PDF，数量: {Count}", htmlList.Count);
        
        // 分批处理，每批次最多处理MaxConcurrentJobs个任务
        var results = new List<byte[]>();
        var batches = htmlList.Chunk(_options.MaxConcurrentJobs);
        
        foreach (var batch in batches)
        {
            var tasks = batch.Select(html => GeneratePdfAsync(html, options)).ToList();
            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);
        }
        
        _logger.LogInformation("批量生成PDF完成，成功数量: {Count}", results.Count);
        return results;
    }

    /// <summary>
    /// 获取服务状态信息
    /// </summary>
    public Task<PdfGenerationServiceStatus> GetStatusAsync()
    {
        var status = new PdfGenerationServiceStatus
        {
            IsInitialized = _isInitialized,
            ActiveTasks = _activeTasks.Count,
            PoolSize = _options.BrowserPoolSize,
            AvailableBrowsers = _options.BrowserPoolSize - _activeTasks.Count,
            Uptime = _uptime.Elapsed,
            TotalTasksProcessed = _totalTasksProcessed,
            FailedTasks = _failedTasks
        };
        
        return Task.FromResult(status);
    }

    /// <summary>
    /// 确保服务已初始化
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new PdfGenerationNotInitializedException();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        await _browserPool.DisposeAsync();
        _logger.LogInformation("PDF生成服务已释放");
    }
}