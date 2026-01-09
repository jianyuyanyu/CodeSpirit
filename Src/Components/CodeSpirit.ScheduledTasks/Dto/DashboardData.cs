using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 仪表板数据
/// </summary>
public class DashboardData
{
    /// <summary>
    /// 任务统计信息
    /// </summary>
    public TaskStatistics Statistics { get; set; } = new();

    /// <summary>
    /// 执行趋势数据（最近N天）
    /// </summary>
    public List<ExecutionTrendItem> ExecutionTrend { get; set; } = new();

    /// <summary>
    /// 执行状态分布（用于饼图）
    /// </summary>
    public List<ChartDataItem> StatusDistribution { get; set; } = new();

    /// <summary>
    /// 任务类型分布（用于饼图）
    /// </summary>
    public List<ChartDataItem> TypeDistribution { get; set; } = new();

    /// <summary>
    /// 最近执行记录
    /// </summary>
    public List<TaskExecution> RecentExecutions { get; set; } = new();
}
