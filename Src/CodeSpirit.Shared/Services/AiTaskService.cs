using System.Collections.Concurrent;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Dtos.AI;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// AI任务服务实现（内存存储版本）
/// </summary>
/// <remarks>
/// 这是一个基于内存的简单实现，适用于单实例部署。
/// 对于分布式部署，建议使用Redis或数据库存储任务状态。
/// </remarks>
public class AiTaskService : IAiTaskService, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, AiTaskStatusDto> _tasks = new();
    private readonly ILogger<AiTaskService> _logger;

    /// <summary>
    /// 初始化AI任务服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public AiTaskService(ILogger<AiTaskService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 创建AI任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="parameters">任务参数</param>
    /// <returns>任务ID</returns>
    public Task<string> CreateTaskAsync(string taskType, object parameters)
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

        _tasks.TryAdd(taskId, task);
        _logger.LogInformation("AI任务已创建：{TaskId}，类型：{TaskType}", taskId, taskType);

        return Task.FromResult(taskId);
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态</returns>
    public Task<AiTaskStatusDto?> GetTaskStatusAsync(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        
        // 计算已耗时
        if (task != null)
        {
            var elapsed = (task.EndTime ?? DateTime.UtcNow) - task.StartTime;
            task.ElapsedTime = FormatElapsedTime(elapsed);
        }

        return Task.FromResult(task);
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="status">新状态</param>
    /// <param name="step">当前步骤</param>
    /// <param name="progress">进度百分比</param>
    /// <param name="message">状态消息</param>
    public Task UpdateTaskStatusAsync(string taskId, AiTaskStatus status, int step = 0, int progress = 0, string? message = null)
    {
        if (_tasks.TryGetValue(taskId, out var task))
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

            _logger.LogInformation("AI任务状态已更新：{TaskId}，状态：{Status}，步骤：{Step}，进度：{Progress}%", 
                taskId, status, step, progress);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加任务日志
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="message">日志消息</param>
    public Task AddTaskLogAsync(string taskId, string message)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            task.Logs.Add(logEntry);
            
            // 保持日志数量在合理范围内
            if (task.Logs.Count > 1000)
            {
                task.Logs.RemoveAt(0);
            }

            _logger.LogDebug("AI任务日志已添加：{TaskId}，消息：{Message}", taskId, message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="result">任务结果</param>
    /// <param name="detailUrl">详情页面URL（可选）</param>
    public async Task CompleteTaskAsync(string taskId, object result, string? detailUrl = null)
    {
        if (_tasks.TryGetValue(taskId, out var task))
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
        if (_tasks.TryGetValue(taskId, out var task))
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
        var cutoff = DateTime.UtcNow.AddHours(-expiredHours);
        var expiredTasks = _tasks.Where(kvp => kvp.Value.StartTime < cutoff).ToList();

        foreach (var expiredTask in expiredTasks)
        {
            _tasks.TryRemove(expiredTask.Key, out _);
            _logger.LogDebug("已清理过期AI任务：{TaskId}", expiredTask.Key);
        }

        if (expiredTasks.Count > 0)
        {
            _logger.LogInformation("已清理 {Count} 个过期AI任务", expiredTasks.Count);
        }

        return Task.CompletedTask;
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
