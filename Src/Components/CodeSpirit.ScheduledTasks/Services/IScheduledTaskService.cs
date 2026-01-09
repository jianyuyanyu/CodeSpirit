using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.ScheduledTasks.Dto;
using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ScheduledTasks.Services;

/// <summary>
/// 定时任务服务接口
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>
    /// 创建定时任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的任务</returns>
    Task<ScheduledTask> CreateTaskAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新定时任务
    /// </summary>
    /// <param name="task">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的任务</returns>
    Task<ScheduledTask?> UpdateTaskAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启用成功</returns>
    Task<bool> EnableTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否禁用成功</returns>
    Task<bool> DisableTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取定时任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务信息</returns>
    Task<ScheduledTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有定时任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<ScheduledTask>> GetAllTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用的定时任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务列表</returns>
    Task<List<ScheduledTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 手动触发任务执行
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行ID</returns>
    Task<string> TriggerTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消任务执行
    /// </summary>
    /// <param name="executionId">执行ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否取消成功</returns>
    Task<bool> CancelExecutionAsync(string executionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务的下次执行时间
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateNextExecuteTimeAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从配置文件加载任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>加载的任务数量</returns>
    Task<int> LoadTasksFromConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 定时任务查询服务接口
/// </summary>
public interface IScheduledTaskQueryService
{
    /// <summary>
    /// 分页查询定时任务
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageList<ScheduledTask>> GetTasksPagedAsync(TaskQueryDto queryDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务执行历史
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行历史</returns>
    Task<PageList<TaskExecution>> GetExecutionHistoryAsync(string taskId, QueryDtoBase queryDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有执行历史
    /// </summary>
    /// <param name="queryDto">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行历史</returns>
    Task<PageList<TaskExecution>> GetAllExecutionHistoryAsync(ExecutionQueryDto queryDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<TaskStatistics> GetTaskStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取正在执行的任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行中的任务</returns>
    Task<List<TaskExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取仪表板数据
    /// </summary>
    /// <param name="days">趋势数据天数（默认7天）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仪表板数据</returns>
    Task<DashboardData> GetDashboardDataAsync(int days = 7, CancellationToken cancellationToken = default);
}
