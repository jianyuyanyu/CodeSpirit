using Microsoft.Extensions.Logging;

namespace CodeSpirit.LLM.Processors;

/// <summary>
/// 默认LLM批量处理器实现
/// </summary>
public class DefaultLLMBatchProcessor : ILLMBatchProcessor
{
    private readonly ILogger<DefaultLLMBatchProcessor> _logger;

    /// <summary>
    /// 初始化默认LLM批量处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DefaultLLMBatchProcessor(ILogger<DefaultLLMBatchProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 批量处理项目
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="items">待处理的项目列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">批量处理选项</param>
    /// <returns>处理结果列表</returns>
    public async Task<List<TResult>> ProcessBatchAsync<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions? options = null)
    {
        var result = await ProcessBatchWithRetryAsync(items, processor, options);
        return result.SuccessResults;
    }

    /// <summary>
    /// 带重试的处理操作
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="operation">操作函数</param>
    /// <param name="options">重试选项</param>
    /// <returns>处理结果</returns>
    public async Task<TResult> ProcessWithRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        RetryOptions? options = null)
    {
        options ??= new RetryOptions();
        var currentAttempt = 0;
        Exception? lastException = null;

        while (currentAttempt <= options.MaxRetries)
        {
            try
            {
                currentAttempt++;
                _logger.LogDebug("开始第 {Attempt}/{MaxAttempts} 次尝试", currentAttempt, options.MaxRetries + 1);

                var result = await operation();
                
                if (currentAttempt > 1)
                {
                    _logger.LogInformation("操作在第 {Attempt} 次尝试后成功", currentAttempt);
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "第 {Attempt}/{MaxAttempts} 次尝试失败: {Message}", 
                    currentAttempt, options.MaxRetries + 1, ex.Message);

                // 检查是否应该重试
                if (options.ShouldRetry != null && !options.ShouldRetry(ex))
                {
                    _logger.LogWarning("根据重试条件判断，不应重试此异常");
                    break;
                }

                // 如果还有重试机会，等待一段时间
                if (currentAttempt <= options.MaxRetries)
                {
                    var delay = options.UseIncrementalDelay 
                        ? TimeSpan.FromMilliseconds(options.RetryDelay.TotalMilliseconds * currentAttempt)
                        : options.RetryDelay;

                    _logger.LogDebug("等待 {Delay}ms 后进行下次重试", delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError(lastException, "操作在 {MaxRetries} 次重试后仍然失败", options.MaxRetries);
        throw lastException ?? new InvalidOperationException("操作失败且无异常信息");
    }

    /// <summary>
    /// 带重试的批量处理
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="items">待处理的项目列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">批量处理选项</param>
    /// <returns>批量处理结果</returns>
    public async Task<BatchProcessingResult<TResult>> ProcessBatchWithRetryAsync<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions? options = null)
    {
        options ??= new BatchProcessingOptions();
        var startTime = DateTime.UtcNow;

        if (items == null || !items.Any())
        {
            _logger.LogWarning("待处理的项目列表为空");
            return BatchProcessingResult<TResult>.Success(
                new List<TResult>(), 0, 0, 0, startTime, DateTime.UtcNow);
        }

        var totalBatches = (int)Math.Ceiling((double)items.Count / options.BatchSize);
        var allResults = new List<TResult>();
        var failedBatches = new List<BatchFailureInfo>();
        var successBatches = 0;

        _logger.LogInformation("开始批量处理：总计 {TotalItems} 个项目，分为 {TotalBatches} 批处理，每批最多 {BatchSize} 个项目",
            items.Count, totalBatches, options.BatchSize);

        // 创建批次
        var batches = CreateBatches(items, options.BatchSize);

        if (options.ConcurrentBatches > 0)
        {
            // 并发处理
            successBatches = await ProcessBatchesConcurrently(batches, processor, options, allResults, failedBatches);
        }
        else
        {
            // 串行处理
            successBatches = await ProcessBatchesSequentially(batches, processor, options, allResults, failedBatches);
        }

        var endTime = DateTime.UtcNow;
        var failureCount = failedBatches.Sum(f => f.BatchSize);

        _logger.LogInformation("批量处理完成：总计 {TotalItems} 个项目，成功 {SuccessCount} 个，失败 {FailureCount} 个，" +
                              "成功批次 {SuccessBatches}/{TotalBatches}，耗时 {Duration}ms",
            items.Count, allResults.Count, failureCount, successBatches, totalBatches, (endTime - startTime).TotalMilliseconds);

        if (failedBatches.Any())
        {
            return BatchProcessingResult<TResult>.PartialSuccess(
                allResults, failedBatches, items.Count, failureCount, 
                totalBatches, successBatches, startTime, endTime);
        }

        return BatchProcessingResult<TResult>.Success(
            allResults, items.Count, totalBatches, successBatches, startTime, endTime);
    }

    /// <summary>
    /// 创建批次
    /// </summary>
    /// <typeparam name="T">项目类型</typeparam>
    /// <param name="items">项目列表</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>批次列表</returns>
    private List<List<T>> CreateBatches<T>(List<T> items, int batchSize)
    {
        var batches = new List<List<T>>();
        
        for (int i = 0; i < items.Count; i += batchSize)
        {
            var batch = items.Skip(i).Take(batchSize).ToList();
            batches.Add(batch);
        }

        return batches;
    }

    /// <summary>
    /// 串行处理批次
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="batches">批次列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">处理选项</param>
    /// <param name="allResults">所有结果列表</param>
    /// <param name="failedBatches">失败批次列表</param>
    /// <returns>成功批次数</returns>
    private async Task<int> ProcessBatchesSequentially<TInput, TResult>(
        List<List<TInput>> batches,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions options,
        List<TResult> allResults,
        List<BatchFailureInfo> failedBatches)
    {
        var successCount = 0;
        
        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var batch = batches[batchIndex];
            var startIndex = batchIndex * options.BatchSize;

            _logger.LogDebug("处理第 {CurrentBatch}/{TotalBatches} 批，项目范围：{StartIndex}-{EndIndex}，共 {BatchCount} 个项目",
                batchIndex + 1, batches.Count, startIndex + 1, startIndex + batch.Count, batch.Count);

            try
            {
                var retryOptions = new RetryOptions
                {
                    MaxRetries = options.MaxRetries,
                    RetryDelay = options.RetryDelay,
                    UseIncrementalDelay = options.UseIncrementalDelay
                };

                var batchResults = await ProcessWithRetryAsync(() => processor(batch), retryOptions);
                allResults.AddRange(batchResults);
                successCount++;

                _logger.LogDebug("第 {CurrentBatch}/{TotalBatches} 批处理完成，获得 {ResultCount} 个结果",
                    batchIndex + 1, batches.Count, batchResults.Count);

                // 如果不是最后一批，添加延迟
                if (batchIndex < batches.Count - 1 && options.DelayBetweenBatches > TimeSpan.Zero)
                {
                    await Task.Delay(options.DelayBetweenBatches);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "第 {CurrentBatch}/{TotalBatches} 批处理失败", batchIndex + 1, batches.Count);

                var failureInfo = new BatchFailureInfo
                {
                    BatchIndex = batchIndex,
                    BatchSize = batch.Count,
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    RetryCount = options.MaxRetries,
                    FailureTime = DateTime.UtcNow
                };

                failedBatches.Add(failureInfo);

                // 根据配置决定是否继续处理
                if (!options.ContinueOnFailure)
                {
                    _logger.LogWarning("由于配置不允许失败后继续，停止处理剩余批次");
                    break;
                }
            }
        }
        
        return successCount;
    }

    /// <summary>
    /// 并发处理批次
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="batches">批次列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">处理选项</param>
    /// <param name="allResults">所有结果列表</param>
    /// <param name="failedBatches">失败批次列表</param>
    /// <returns>成功批次数</returns>
    private async Task<int> ProcessBatchesConcurrently<TInput, TResult>(
        List<List<TInput>> batches,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions options,
        List<TResult> allResults,
        List<BatchFailureInfo> failedBatches)
    {
        var semaphore = new SemaphoreSlim(options.ConcurrentBatches, options.ConcurrentBatches);
        var tasks = new List<Task>();
        var resultsLock = new object();
        var successCount = 0;

        _logger.LogInformation("使用并发处理，最大并发数: {ConcurrentBatches}", options.ConcurrentBatches);

        for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            var currentBatchIndex = batchIndex;
            var batch = batches[batchIndex];

            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var startIndex = currentBatchIndex * options.BatchSize;
                    _logger.LogDebug("并发处理第 {CurrentBatch}/{TotalBatches} 批，项目范围：{StartIndex}-{EndIndex}，共 {BatchCount} 个项目",
                        currentBatchIndex + 1, batches.Count, startIndex + 1, startIndex + batch.Count, batch.Count);

                    var retryOptions = new RetryOptions
                    {
                        MaxRetries = options.MaxRetries,
                        RetryDelay = options.RetryDelay,
                        UseIncrementalDelay = options.UseIncrementalDelay
                    };

                    var batchResults = await ProcessWithRetryAsync(() => processor(batch), retryOptions);

                    lock (resultsLock)
                    {
                        allResults.AddRange(batchResults);
                        successCount++;
                    }

                    _logger.LogDebug("第 {CurrentBatch}/{TotalBatches} 批并发处理完成，获得 {ResultCount} 个结果",
                        currentBatchIndex + 1, batches.Count, batchResults.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "第 {CurrentBatch}/{TotalBatches} 批并发处理失败", currentBatchIndex + 1, batches.Count);

                    var failureInfo = new BatchFailureInfo
                    {
                        BatchIndex = currentBatchIndex,
                        BatchSize = batch.Count,
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        RetryCount = options.MaxRetries,
                        FailureTime = DateTime.UtcNow
                    };

                    lock (resultsLock)
                    {
                        failedBatches.Add(failureInfo);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("所有并发批次处理完成");
        
        return successCount;
    }
}
