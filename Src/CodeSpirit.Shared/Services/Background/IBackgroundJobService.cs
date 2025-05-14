using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Shared.Services.Background;

/// <summary>
/// 后台任务服务接口
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// 将任务添加到队列
    /// </summary>
    /// <param name="job">要执行的任务</param>
    /// <returns>任务执行的标识符</returns>
    Task<string> EnqueueAsync(Func<IServiceScopeFactory, CancellationToken, Task> job);
    
    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="jobId">任务ID</param>
    /// <returns>任务状态</returns>
    Task<JobStatus> GetStatusAsync(string jobId);
}

/// <summary>
/// 任务状态
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// 已排队
    /// </summary>
    Queued,
    
    /// <summary>
    /// 正在执行
    /// </summary>
    Running,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
    
    /// <summary>
    /// 失败
    /// </summary>
    Failed,
    
    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled
} 