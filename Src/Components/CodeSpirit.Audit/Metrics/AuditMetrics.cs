using System.Collections.Concurrent;
using System.Threading;

namespace CodeSpirit.Audit.Metrics;

/// <summary>
/// 审计指标收集器
/// </summary>
/// <remarks>
/// 记录审计组件的关键性能指标和统计信息
/// </remarks>
public class AuditMetrics
{
    private long _totalAuditLogs = 0;
    private long _successfulAuditLogs = 0;
    private long _failedAuditLogs = 0;
    private long _rabbitMQSuccessCount = 0;
    private long _rabbitMQFailureCount = 0;
    private long _storageSuccessCount = 0;
    private long _storageFailureCount = 0;
    private long _degradationCount = 0;
    private long _completeFailureCount = 0;
    private long _totalProcessingTime = 0;
    private long _batchStoredCount = 0;
    private long _batchStoredSize = 0;

    /// <summary>
    /// 审计日志总数
    /// </summary>
    public long TotalAuditLogs => Interlocked.Read(ref _totalAuditLogs);

    /// <summary>
    /// 成功的审计日志数
    /// </summary>
    public long SuccessfulAuditLogs => Interlocked.Read(ref _successfulAuditLogs);

    /// <summary>
    /// 失败的审计日志数
    /// </summary>
    public long FailedAuditLogs => Interlocked.Read(ref _failedAuditLogs);

    /// <summary>
    /// RabbitMQ 发送成功次数
    /// </summary>
    public long RabbitMQSuccessCount => Interlocked.Read(ref _rabbitMQSuccessCount);

    /// <summary>
    /// RabbitMQ 发送失败次数
    /// </summary>
    public long RabbitMQFailureCount => Interlocked.Read(ref _rabbitMQFailureCount);

    /// <summary>
    /// 存储成功次数
    /// </summary>
    public long StorageSuccessCount => Interlocked.Read(ref _storageSuccessCount);

    /// <summary>
    /// 存储失败次数
    /// </summary>
    public long StorageFailureCount => Interlocked.Read(ref _storageFailureCount);

    /// <summary>
    /// 降级触发次数（RabbitMQ 降级到直接存储）
    /// </summary>
    public long DegradationCount => Interlocked.Read(ref _degradationCount);

    /// <summary>
    /// 完全失败次数（记录日志场景）
    /// </summary>
    public long CompleteFailureCount => Interlocked.Read(ref _completeFailureCount);

    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime => _totalAuditLogs > 0 
        ? (double)Interlocked.Read(ref _totalProcessingTime) / _totalAuditLogs 
        : 0;

    /// <summary>
    /// 批量存储次数
    /// </summary>
    public long BatchStoredCount => Interlocked.Read(ref _batchStoredCount);

    /// <summary>
    /// 批量存储总大小
    /// </summary>
    public long BatchStoredSize => Interlocked.Read(ref _batchStoredSize);

    /// <summary>
    /// 记录审计日志总数
    /// </summary>
    public void IncrementTotalAuditLogs()
    {
        Interlocked.Increment(ref _totalAuditLogs);
    }

    /// <summary>
    /// 记录成功的审计日志
    /// </summary>
    public void IncrementSuccessfulAuditLogs()
    {
        Interlocked.Increment(ref _successfulAuditLogs);
    }

    /// <summary>
    /// 记录失败的审计日志
    /// </summary>
    public void IncrementFailedAuditLogs()
    {
        Interlocked.Increment(ref _failedAuditLogs);
    }

    /// <summary>
    /// 记录 RabbitMQ 发送成功
    /// </summary>
    public void IncrementRabbitMQSuccess()
    {
        Interlocked.Increment(ref _rabbitMQSuccessCount);
    }

    /// <summary>
    /// 记录 RabbitMQ 发送失败
    /// </summary>
    public void IncrementRabbitMQFailure()
    {
        Interlocked.Increment(ref _rabbitMQFailureCount);
    }

    /// <summary>
    /// 记录存储成功
    /// </summary>
    public void IncrementStorageSuccess()
    {
        Interlocked.Increment(ref _storageSuccessCount);
    }

    /// <summary>
    /// 记录存储失败
    /// </summary>
    public void IncrementStorageFailure()
    {
        Interlocked.Increment(ref _storageFailureCount);
    }

    /// <summary>
    /// 记录降级触发
    /// </summary>
    public void IncrementDegradation()
    {
        Interlocked.Increment(ref _degradationCount);
    }

    /// <summary>
    /// 记录完全失败
    /// </summary>
    public void IncrementCompleteFailure()
    {
        Interlocked.Increment(ref _completeFailureCount);
    }

    /// <summary>
    /// 添加处理时间
    /// </summary>
    /// <param name="milliseconds">处理时间（毫秒）</param>
    public void AddProcessingTime(long milliseconds)
    {
        Interlocked.Add(ref _totalProcessingTime, milliseconds);
    }

    /// <summary>
    /// 记录批量存储
    /// </summary>
    /// <param name="batchSize">批次大小</param>
    public void RecordBatchStored(int batchSize)
    {
        Interlocked.Increment(ref _batchStoredCount);
        Interlocked.Add(ref _batchStoredSize, batchSize);
    }

    /// <summary>
    /// 重置所有指标
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalAuditLogs, 0);
        Interlocked.Exchange(ref _successfulAuditLogs, 0);
        Interlocked.Exchange(ref _failedAuditLogs, 0);
        Interlocked.Exchange(ref _rabbitMQSuccessCount, 0);
        Interlocked.Exchange(ref _rabbitMQFailureCount, 0);
        Interlocked.Exchange(ref _storageSuccessCount, 0);
        Interlocked.Exchange(ref _storageFailureCount, 0);
        Interlocked.Exchange(ref _degradationCount, 0);
        Interlocked.Exchange(ref _completeFailureCount, 0);
        Interlocked.Exchange(ref _totalProcessingTime, 0);
        Interlocked.Exchange(ref _batchStoredCount, 0);
        Interlocked.Exchange(ref _batchStoredSize, 0);
    }

    /// <summary>
    /// 获取指标快照
    /// </summary>
    public AuditMetricsSnapshot GetSnapshot()
    {
        return new AuditMetricsSnapshot
        {
            TotalAuditLogs = TotalAuditLogs,
            SuccessfulAuditLogs = SuccessfulAuditLogs,
            FailedAuditLogs = FailedAuditLogs,
            RabbitMQSuccessCount = RabbitMQSuccessCount,
            RabbitMQFailureCount = RabbitMQFailureCount,
            StorageSuccessCount = StorageSuccessCount,
            StorageFailureCount = StorageFailureCount,
            DegradationCount = DegradationCount,
            CompleteFailureCount = CompleteFailureCount,
            AverageProcessingTime = AverageProcessingTime,
            BatchStoredCount = BatchStoredCount,
            BatchStoredSize = BatchStoredSize,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// 审计指标快照
/// </summary>
public class AuditMetricsSnapshot
{
    /// <summary>
    /// 审计日志总数
    /// </summary>
    public long TotalAuditLogs { get; set; }

    /// <summary>
    /// 成功的审计日志数
    /// </summary>
    public long SuccessfulAuditLogs { get; set; }

    /// <summary>
    /// 失败的审计日志数
    /// </summary>
    public long FailedAuditLogs { get; set; }

    /// <summary>
    /// RabbitMQ 发送成功次数
    /// </summary>
    public long RabbitMQSuccessCount { get; set; }

    /// <summary>
    /// RabbitMQ 发送失败次数
    /// </summary>
    public long RabbitMQFailureCount { get; set; }

    /// <summary>
    /// 存储成功次数
    /// </summary>
    public long StorageSuccessCount { get; set; }

    /// <summary>
    /// 存储失败次数
    /// </summary>
    public long StorageFailureCount { get; set; }

    /// <summary>
    /// 降级触发次数
    /// </summary>
    public long DegradationCount { get; set; }

    /// <summary>
    /// 完全失败次数
    /// </summary>
    public long CompleteFailureCount { get; set; }

    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime { get; set; }

    /// <summary>
    /// 批量存储次数
    /// </summary>
    public long BatchStoredCount { get; set; }

    /// <summary>
    /// 批量存储总大小
    /// </summary>
    public long BatchStoredSize { get; set; }

    /// <summary>
    /// 快照时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}
