#nullable enable
using System.Collections.Concurrent;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Dtos.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// AI任务服务实现（分布式缓存版本）
/// </summary>
/// <remarks>
/// 基于IDistributedCache实现，支持多实例部署和数据持久化。
/// 适用于分布式部署环境，任务状态存储在分布式缓存中。
/// </remarks>
public class AiTaskService : IAiTaskService, ISingletonDependency
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AiTaskService> _logger;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;
    
    // 缓存键前缀
    private const string TaskKeyPrefix = "AiTask:";

    /// <summary>
    /// 初始化AI任务服务
    /// </summary>
    /// <param name="distributedCache">分布式缓存</param>
    /// <param name="logger">日志记录器</param>
    public AiTaskService(IDistributedCache distributedCache, ILogger<AiTaskService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // 配置默认缓存选项 - 24小时绝对过期，12小时滑动过期
        _defaultCacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(12)
        };
    }

    /// <summary>
    /// 创建AI任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="parameters">任务参数</param>
    /// <returns>任务ID</returns>
    public async Task<string> CreateTaskAsync(string taskType, object parameters)
    {
        string taskId = Guid.NewGuid().ToString("N");
        
        var task = new AiTaskStatusDto
        {
            TaskId = taskId,
            Status = AiTaskStatus.Pending,
            StatusText = "任务已创建，等待开始",
            Step = 0,
            Progress = 0,
            StartTime = DateTime.UtcNow,
            Logs = new List<string> { $"[{DateTime.Now:HH:mm:ss}] 任务已创建，类型：{taskType}" }
        };

        await SetTaskToCache(taskId, task);
        _logger.LogInformation("AI任务已创建：{TaskId}，类型：{TaskType}", taskId, taskType);

        return taskId;
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态</returns>
    public async Task<AiTaskStatusDto?> GetTaskStatusAsync(string taskId)
    {
        var task = await GetTaskFromCache(taskId);
        
        // 计算已耗时
        if (task != null)
        {
            var elapsed = (task.EndTime ?? DateTime.UtcNow) - task.StartTime;
            task.ElapsedTime = FormatElapsedTime(elapsed);
        }

        return task;
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="status">新状态</param>
    /// <param name="step">当前步骤</param>
    /// <param name="progress">进度百分比</param>
    /// <param name="message">状态消息</param>
    public async Task UpdateTaskStatusAsync(string taskId, AiTaskStatus status, int step = 0, int progress = 0, string? message = null)
    {
        var task = await GetTaskFromCache(taskId);
        if (task != null)
        {
            task.Status = status;
            task.Step = step;
            task.Progress = progress;

            if (!string.IsNullOrEmpty(message))
            {
                task.StatusText = message;
            }
            else
            {
                task.StatusText = status switch
                {
                    AiTaskStatus.Pending => "等待开始",
                    AiTaskStatus.Running => "正在处理中...",
                    AiTaskStatus.Completed => "处理完成",
                    AiTaskStatus.Failed => "处理失败",
                    AiTaskStatus.Cancelled => "已取消",
                    _ => "未知状态"
                };
            }

            if (status == AiTaskStatus.Completed || status == AiTaskStatus.Failed || status == AiTaskStatus.Cancelled)
            {
                task.EndTime = DateTime.UtcNow;
            }

            await SetTaskToCache(taskId, task);
            _logger.LogInformation("AI任务状态已更新：{TaskId}，状态：{Status}，步骤：{Step}，进度：{Progress}%", 
                taskId, status, step, progress);
        }
    }

    /// <summary>
    /// 添加任务日志
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="message">日志消息</param>
    public async Task AddTaskLogAsync(string taskId, string message)
    {
        var task = await GetTaskFromCache(taskId);
        if (task != null)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            task.Logs.Add(logEntry);
            
            // 保持日志数量在合理范围内
            if (task.Logs.Count > 1000)
            {
                task.Logs.RemoveAt(0);
            }

            await SetTaskToCache(taskId, task);
            _logger.LogDebug("AI任务日志已添加：{TaskId}，消息：{Message}", taskId, message);
        }
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="result">任务结果</param>
    /// <param name="detailUrl">详情页面URL（可选）</param>
    public async Task CompleteTaskAsync(string taskId, object result, string? detailUrl = null)
    {
        var task = await GetTaskFromCache(taskId);
        if (task != null)
        {
            task.Result = result;
            task.DetailUrl = detailUrl;
            
            await UpdateTaskStatusAsync(taskId, AiTaskStatus.Completed, 4, 100, "任务已成功完成");
            await AddTaskLogAsync(taskId, "任务执行成功完成");

            _logger.LogInformation("AI任务已完成：{TaskId}", taskId);
        }
    }

    /// <summary>
    /// 任务失败
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="errorMessage">错误消息</param>
    public async Task FailTaskAsync(string taskId, string errorMessage)
    {
        var task = await GetTaskFromCache(taskId);
        if (task != null)
        {
            task.ErrorMessage = errorMessage;
            
            await UpdateTaskStatusAsync(taskId, AiTaskStatus.Failed, task.Step, task.Progress, $"任务失败：{errorMessage}");
            await AddTaskLogAsync(taskId, $"错误：{errorMessage}");

            _logger.LogError("AI任务失败：{TaskId}，错误：{Error}", taskId, errorMessage);
        }
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public async Task CancelTaskAsync(string taskId)
    {
        await UpdateTaskStatusAsync(taskId, AiTaskStatus.Cancelled, 0, 0, "任务已被取消");
        await AddTaskLogAsync(taskId, "任务已被用户取消");

        _logger.LogInformation("AI任务已取消：{TaskId}", taskId);
    }

    /// <summary>
    /// 清理过期任务
    /// </summary>
    /// <param name="expiredHours">过期小时数，默认24小时</param>
    public Task CleanupExpiredTasksAsync(int expiredHours = 24)
    {
        // 注意：这个方法在分布式缓存中的实现相对复杂
        // 因为IDistributedCache没有提供列出所有键的功能
        // 在实际生产环境中，建议使用Redis的SCAN命令或专门的任务清理机制
        // 这里仅作为接口实现，实际清理由缓存的过期机制自动处理
        
        _logger.LogInformation("清理过期任务请求已接收，过期时间：{ExpiredHours}小时。实际清理由分布式缓存的过期机制自动处理。", expiredHours);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 从缓存中获取任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态</returns>
    private async Task<AiTaskStatusDto?> GetTaskFromCache(string taskId)
    {
        try
        {
            string cacheKey = TaskKeyPrefix + taskId;
            var cachedData = await _distributedCache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(cachedData))
            {
                return null;
            }
            
            return JsonConvert.DeserializeObject<AiTaskStatusDto>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从缓存获取AI任务失败：{TaskId}", taskId);
            return null;
        }
    }
    
    /// <summary>
    /// 将任务保存到缓存
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="task">任务状态</param>
    private async Task SetTaskToCache(string taskId, AiTaskStatusDto task)
    {
        try
        {
            string cacheKey = TaskKeyPrefix + taskId;
            string json = JsonConvert.SerializeObject(task);
            await _distributedCache.SetStringAsync(cacheKey, json, _defaultCacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存AI任务到缓存失败：{TaskId}", taskId);
        }
    }

    /// <summary>
    /// 格式化已耗时
    /// </summary>
    /// <param name="elapsed">时间间隔</param>
    /// <returns>格式化的时间字符串</returns>
    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
        {
            return $"{(int)elapsed.TotalHours}小时{elapsed.Minutes}分{elapsed.Seconds}秒";
        }
        else if (elapsed.TotalMinutes >= 1)
        {
            return $"{elapsed.Minutes}分{elapsed.Seconds}秒";
        }
        else
        {
            return $"{elapsed.Seconds}秒";
        }
    }
}
