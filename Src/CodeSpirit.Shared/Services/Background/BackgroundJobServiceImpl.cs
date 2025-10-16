using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CodeSpirit.Shared.Services.Background;

/// <summary>
/// 后台任务服务实现
/// </summary>
public class BackgroundJobServiceImpl : BackgroundService, IBackgroundJobService
{
    private readonly ILogger<BackgroundJobServiceImpl> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConcurrentDictionary<string, BackgroundJob> _jobs = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志服务</param>
    /// <param name="serviceScopeFactory">服务范围工厂</param>
    public BackgroundJobServiceImpl(ILogger<BackgroundJobServiceImpl> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// 将任务添加到队列
    /// </summary>
    /// <param name="job">要执行的任务</param>
    /// <returns>任务执行的标识符</returns>
    public Task<string> EnqueueAsync(Func<IServiceScopeFactory, CancellationToken, Task> job)
    {
        var jobId = Guid.NewGuid().ToString();
        var backgroundJob = new BackgroundJob
        {
            Id = jobId,
            Job = job,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow
        };

        _jobs.TryAdd(jobId, backgroundJob);
        _logger.LogInformation("已将任务 {JobId} 添加到队列", jobId);
        
        return Task.FromResult(jobId);
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="jobId">任务ID</param>
    /// <returns>任务状态</returns>
    public Task<JobStatus> GetStatusAsync(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            return Task.FromResult(job.Status);
        }

        return Task.FromResult(JobStatus.Failed);
    }

    /// <summary>
    /// 在后台执行任务
    /// </summary>
    /// <param name="stoppingToken">取消令牌</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("后台任务服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 查找队列中的任务
                var queuedJobs = _jobs.Values
                    .Where(j => j.Status == JobStatus.Queued)
                    .OrderBy(j => j.CreatedAt)
                    .ToList();

                foreach (var job in queuedJobs)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        _logger.LogInformation("开始执行任务 {JobId}", job.Id);
                        job.Status = JobStatus.Running;
                        job.StartedAt = DateTime.UtcNow;

                        // 执行任务
                        await job.Job(_serviceScopeFactory, stoppingToken);

                        job.Status = JobStatus.Completed;
                        job.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation("任务 {JobId} 已完成", job.Id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        job.Status = JobStatus.Cancelled;
                        _logger.LogWarning("任务 {JobId} 已取消", job.Id);
                    }
                    catch (Exception ex)
                    {
                        job.Status = JobStatus.Failed;
                        job.Error = ex.ToString();
                        _logger.LogError(ex, "任务 {JobId} 执行失败", job.Id);
                    }
                }

                // 清理已完成超过24小时的任务
                var oldJobs = _jobs.Values
                    .Where(j => (j.Status == JobStatus.Completed || j.Status == JobStatus.Failed || j.Status == JobStatus.Cancelled) 
                             && j.CompletedAt.HasValue 
                             && (DateTime.UtcNow - j.CompletedAt.Value).TotalHours > 24)
                    .Select(j => j.Id)
                    .ToList();

                foreach (var jobId in oldJobs)
                {
                    _jobs.TryRemove(jobId, out _);
                    _logger.LogInformation("已清理任务 {JobId}", jobId);
                }

                // 等待一定时间后再次检查队列
                await Task.Delay(1000, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台任务处理过程中发生错误");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("后台任务服务已停止");
    }
}