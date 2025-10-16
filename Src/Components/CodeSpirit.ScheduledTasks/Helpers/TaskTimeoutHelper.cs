namespace CodeSpirit.ScheduledTasks.Helpers;

/// <summary>
/// 任务超时控制辅助类
/// </summary>
public static class TaskTimeoutHelper
{
    /// <summary>
    /// 创建带超时的取消令牌
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <param name="parentToken">父级取消令牌</param>
    /// <returns>取消令牌源</returns>
    public static CancellationTokenSource CreateTimeoutToken(TimeSpan timeout, CancellationToken parentToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        cts.CancelAfter(timeout);
        return cts;
    }

    /// <summary>
    /// 执行带超时的任务
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务结果</returns>
    /// <exception cref="TimeoutException">任务超时异常</exception>
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CreateTimeoutToken(timeout, cancellationToken);
        
        try
        {
            return await task(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"任务执行超时，超时时间: {timeout}");
        }
    }

    /// <summary>
    /// 执行带超时的任务（无返回值）
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="TimeoutException">任务超时异常</exception>
    public static async Task ExecuteWithTimeoutAsync(
        Func<CancellationToken, Task> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CreateTimeoutToken(timeout, cancellationToken);
        
        try
        {
            await task(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"任务执行超时，超时时间: {timeout}");
        }
    }

    /// <summary>
    /// 执行带超时和重试的任务
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="retryInterval">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务结果</returns>
    public static async Task<T> ExecuteWithTimeoutAndRetryAsync<T>(
        Func<CancellationToken, Task<T>> task,
        TimeSpan timeout,
        int maxRetryCount = 3,
        TimeSpan? retryInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = retryInterval ?? TimeSpan.FromSeconds(1);
        var lastException = default(Exception);

        for (int attempt = 0; attempt <= maxRetryCount; attempt++)
        {
            try
            {
                return await ExecuteWithTimeoutAsync(task, timeout, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetryCount)
            {
                lastException = ex;
                
                if (interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, cancellationToken);
                }
            }
        }

        throw lastException ?? new InvalidOperationException("任务执行失败");
    }

    /// <summary>
    /// 执行带超时和重试的任务（无返回值）
    /// </summary>
    /// <param name="task">要执行的任务</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="retryInterval">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task ExecuteWithTimeoutAndRetryAsync(
        Func<CancellationToken, Task> task,
        TimeSpan timeout,
        int maxRetryCount = 3,
        TimeSpan? retryInterval = null,
        CancellationToken cancellationToken = default)
    {
        var interval = retryInterval ?? TimeSpan.FromSeconds(1);
        var lastException = default(Exception);

        for (int attempt = 0; attempt <= maxRetryCount; attempt++)
        {
            try
            {
                await ExecuteWithTimeoutAsync(task, timeout, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxRetryCount)
            {
                lastException = ex;
                
                if (interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, cancellationToken);
                }
            }
        }

        throw lastException ?? new InvalidOperationException("任务执行失败");
    }

    /// <summary>
    /// 检查是否为超时异常
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否为超时异常</returns>
    public static bool IsTimeoutException(Exception exception)
    {
        return exception is TimeoutException ||
               (exception is OperationCanceledException && exception.Message.Contains("超时"));
    }

    /// <summary>
    /// 获取推荐的超时时间
    /// </summary>
    /// <param name="estimatedDuration">预估执行时间</param>
    /// <param name="safetyFactor">安全系数（默认2倍）</param>
    /// <returns>推荐超时时间</returns>
    public static TimeSpan GetRecommendedTimeout(TimeSpan estimatedDuration, double safetyFactor = 2.0)
    {
        if (safetyFactor <= 1.0)
        {
            safetyFactor = 2.0;
        }

        var recommendedTimeout = TimeSpan.FromMilliseconds(estimatedDuration.TotalMilliseconds * safetyFactor);
        
        // 最小超时时间为30秒
        if (recommendedTimeout < TimeSpan.FromSeconds(30))
        {
            recommendedTimeout = TimeSpan.FromSeconds(30);
        }
        
        // 最大超时时间为24小时
        if (recommendedTimeout > TimeSpan.FromHours(24))
        {
            recommendedTimeout = TimeSpan.FromHours(24);
        }

        return recommendedTimeout;
    }
}
