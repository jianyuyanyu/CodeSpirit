namespace CodeSpirit.LLM.Processors;

/// <summary>
/// LLM批量处理器接口
/// </summary>
public interface ILLMBatchProcessor
{
    /// <summary>
    /// 批量处理项目
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="items">待处理的项目列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">批量处理选项</param>
    /// <returns>处理结果列表</returns>
    Task<List<TResult>> ProcessBatchAsync<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions? options = null);

    /// <summary>
    /// 带重试的处理操作
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="operation">操作函数</param>
    /// <param name="options">重试选项</param>
    /// <returns>处理结果</returns>
    Task<TResult> ProcessWithRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        RetryOptions? options = null);

    /// <summary>
    /// 带重试的批量处理
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="items">待处理的项目列表</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="options">批量处理选项</param>
    /// <returns>批量处理结果</returns>
    Task<BatchProcessingResult<TResult>> ProcessBatchWithRetryAsync<TInput, TResult>(
        List<TInput> items,
        Func<List<TInput>, Task<List<TResult>>> processor,
        BatchProcessingOptions? options = null);
}

/// <summary>
/// 批量处理选项
/// </summary>
public class BatchProcessingOptions
{
    /// <summary>
    /// 批次大小
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// 批次间延迟
    /// </summary>
    public TimeSpan DelayBetweenBatches { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 重试延迟
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// 是否启用递增延迟
    /// </summary>
    public bool UseIncrementalDelay { get; set; } = true;

    /// <summary>
    /// 失败时是否继续处理其他批次
    /// </summary>
    public bool ContinueOnFailure { get; set; } = true;

    /// <summary>
    /// 并发处理批次数量（0表示串行处理）
    /// </summary>
    public int ConcurrentBatches { get; set; } = 0;
}

/// <summary>
/// 重试选项
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// 重试延迟
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 是否启用递增延迟
    /// </summary>
    public bool UseIncrementalDelay { get; set; } = true;

    /// <summary>
    /// 重试条件判断函数
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; set; }
}

/// <summary>
/// 批量处理结果
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class BatchProcessingResult<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 成功处理的结果列表
    /// </summary>
    public List<T> SuccessResults { get; set; } = new();

    /// <summary>
    /// 失败的批次信息
    /// </summary>
    public List<BatchFailureInfo> FailedBatches { get; set; } = new();

    /// <summary>
    /// 总处理项目数
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// 成功处理的项目数
    /// </summary>
    public int SuccessCount => SuccessResults.Count;

    /// <summary>
    /// 失败的项目数
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 处理的批次数
    /// </summary>
    public int TotalBatches { get; set; }

    /// <summary>
    /// 成功的批次数
    /// </summary>
    public int SuccessBatches { get; set; }

    /// <summary>
    /// 失败的批次数
    /// </summary>
    public int FailedBatchCount => FailedBatches.Count;

    /// <summary>
    /// 处理开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 处理结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 总处理时间
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 创建成功结果
    /// </summary>
    /// <param name="results">成功结果列表</param>
    /// <param name="totalItems">总项目数</param>
    /// <param name="totalBatches">总批次数</param>
    /// <param name="successBatches">成功批次数</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>成功结果</returns>
    public static BatchProcessingResult<T> Success(
        List<T> results, 
        int totalItems, 
        int totalBatches, 
        int successBatches,
        DateTime startTime,
        DateTime endTime)
    {
        return new BatchProcessingResult<T>
        {
            IsSuccess = true,
            SuccessResults = results,
            TotalItems = totalItems,
            TotalBatches = totalBatches,
            SuccessBatches = successBatches,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// 创建部分成功结果
    /// </summary>
    /// <param name="successResults">成功结果列表</param>
    /// <param name="failedBatches">失败批次列表</param>
    /// <param name="totalItems">总项目数</param>
    /// <param name="failureCount">失败项目数</param>
    /// <param name="totalBatches">总批次数</param>
    /// <param name="successBatches">成功批次数</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>部分成功结果</returns>
    public static BatchProcessingResult<T> PartialSuccess(
        List<T> successResults,
        List<BatchFailureInfo> failedBatches,
        int totalItems,
        int failureCount,
        int totalBatches,
        int successBatches,
        DateTime startTime,
        DateTime endTime)
    {
        return new BatchProcessingResult<T>
        {
            IsSuccess = false,
            SuccessResults = successResults,
            FailedBatches = failedBatches,
            TotalItems = totalItems,
            FailureCount = failureCount,
            TotalBatches = totalBatches,
            SuccessBatches = successBatches,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}

/// <summary>
/// 批次失败信息
/// </summary>
public class BatchFailureInfo
{
    /// <summary>
    /// 批次索引
    /// </summary>
    public int BatchIndex { get; set; }

    /// <summary>
    /// 批次大小
    /// </summary>
    public int BatchSize { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 异常详情
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 失败时间
    /// </summary>
    public DateTime FailureTime { get; set; }
}
