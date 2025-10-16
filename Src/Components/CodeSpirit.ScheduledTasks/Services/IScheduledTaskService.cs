using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
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
}

/// <summary>
/// 任务查询DTO
/// </summary>
public class TaskQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务名称
    /// </summary>
    [DisplayName("任务名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [DisplayName("任务状态")]
    public Models.TaskStatus? Status { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [DisplayName("任务类型")]
    public TaskType? Type { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [DisplayName("任务分组")]
    public string? Group { get; set; }

    /// <summary>
    /// 是否来自配置文件
    /// </summary>
    [DisplayName("来自配置文件")]
    public bool? IsFromConfiguration { get; set; }
}

/// <summary>
/// 执行历史查询DTO
/// </summary>
public class ExecutionQueryDto : QueryDtoBase
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [DisplayName("任务ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 执行状态
    /// </summary>
    [DisplayName("执行状态")]
    public Models.TaskStatus? Status { get; set; }

    /// <summary>
    /// 开始时间范围
    /// </summary>
    [DisplayName("开始时间")]
    public DateTime? StartTimeFrom { get; set; }

    /// <summary>
    /// 结束时间范围
    /// </summary>
    [DisplayName("结束时间")]
    public DateTime? StartTimeTo { get; set; }

    /// <summary>
    /// 执行节点
    /// </summary>
    [DisplayName("执行节点")]
    public string? ExecutionNode { get; set; }
}

/// <summary>
/// 任务统计信息
/// </summary>
public class TaskStatistics
{
    /// <summary>
    /// 总任务数
    /// </summary>
    [DisplayName("总任务数")]
    public int TotalTasks { get; set; }

    /// <summary>
    /// 启用任务数
    /// </summary>
    [DisplayName("启用任务数")]
    public int EnabledTasks { get; set; }

    /// <summary>
    /// 禁用任务数
    /// </summary>
    [DisplayName("禁用任务数")]
    public int DisabledTasks { get; set; }

    /// <summary>
    /// 正在执行任务数
    /// </summary>
    [DisplayName("正在执行任务数")]
    public int RunningTasks { get; set; }

    /// <summary>
    /// 今日执行次数
    /// </summary>
    [DisplayName("今日执行次数")]
    public int TodayExecutions { get; set; }

    /// <summary>
    /// 今日成功次数
    /// </summary>
    [DisplayName("今日成功次数")]
    public int TodaySuccessExecutions { get; set; }

    /// <summary>
    /// 今日失败次数
    /// </summary>
    [DisplayName("今日失败次数")]
    public int TodayFailedExecutions { get; set; }

    /// <summary>
    /// 成功率
    /// </summary>
    [DisplayName("成功率")]
    public double SuccessRate { get; set; }

    /// <summary>
    /// 按状态分组统计
    /// </summary>
    [DisplayName("状态统计")]
    public Dictionary<Models.TaskStatus, int> StatusStatistics { get; set; } = new();

    /// <summary>
    /// 按类型分组统计
    /// </summary>
    [DisplayName("类型统计")]
    public Dictionary<TaskType, int> TypeStatistics { get; set; } = new();
}
