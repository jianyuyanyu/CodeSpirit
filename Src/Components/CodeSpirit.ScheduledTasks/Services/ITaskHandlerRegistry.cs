namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 任务处理器注册表接口
/// </summary>
public interface ITaskHandlerRegistry
{
    /// <summary>
    /// 注册任务处理器
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="handlerTypes">处理器类型列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RegisterHandlersAsync(string serviceName, IEnumerable<string> handlerTypes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 注册任务所属服务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="serviceName">服务名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RegisterTaskServiceAsync(string taskId, string serviceName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 查询任务所属服务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务名称，如果未找到则返回null</returns>
    Task<string?> GetTaskServiceNameAsync(string taskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查任务是否属于指定服务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="serviceName">服务名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果任务属于指定服务则返回true</returns>
    Task<bool> IsTaskOwnedByServiceAsync(string taskId, string serviceName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取服务注册的所有处理器类型
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理器类型列表</returns>
    Task<List<string>> GetServiceHandlersAsync(string serviceName, CancellationToken cancellationToken = default);
}

