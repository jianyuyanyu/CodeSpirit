namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务处理器接口
/// </summary>
public interface ITaskHandler
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// 泛型任务处理器接口
/// </summary>
/// <typeparam name="TParameters">参数类型</typeparam>
public interface ITaskHandler<TParameters> : ITaskHandler
    where TParameters : class
{
    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="parameters">强类型参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<string?> ExecuteAsync(TParameters? parameters, CancellationToken cancellationToken = default);
}
