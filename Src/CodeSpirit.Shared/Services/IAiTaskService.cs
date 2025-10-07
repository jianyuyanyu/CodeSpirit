using CodeSpirit.Shared.Dtos.AI;

namespace CodeSpirit.Shared.Services;

/// <summary>
/// AI任务服务接口
/// </summary>
public interface IAiTaskService
{
    /// <summary>
    /// 创建AI任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="parameters">任务参数</param>
    /// <returns>任务ID</returns>
    Task<string> CreateTaskAsync(string taskType, object parameters);


    /// <summary>
    /// 获取任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态</returns>
    Task<AiTaskStatusDto?> GetTaskStatusAsync(string taskId);

    /// <summary>
    /// 更新任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="status">新状态</param>
    /// <param name="step">当前步骤</param>
    /// <param name="progress">进度百分比</param>
    /// <param name="message">状态消息</param>
    /// <param name="result">任务结果</param>
    Task UpdateTaskStatusAsync(string taskId, AiTaskStatus status, int step = 0, int progress = 0, string? message = null, object? result = null);

    /// <summary>
    /// 添加任务日志
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="message">日志消息</param>
    Task AddTaskLogAsync(string taskId, string message);

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="result">任务结果</param>
    /// <param name="detailUrl">详情页面URL（可选）</param>
    Task CompleteTaskAsync(string taskId, object result, string? detailUrl = null);

    /// <summary>
    /// 任务失败
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="errorMessage">错误消息</param>
    Task FailTaskAsync(string taskId, string errorMessage);

    /// <summary>
    /// 取消任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    Task CancelTaskAsync(string taskId);

    /// <summary>
    /// 清理过期任务
    /// </summary>
    /// <param name="expiredHours">过期小时数，默认24小时</param>
    Task CleanupExpiredTasksAsync(int expiredHours = 24);
}
