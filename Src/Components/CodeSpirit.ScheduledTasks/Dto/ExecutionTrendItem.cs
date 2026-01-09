namespace CodeSpirit.ScheduledTasks.Dto;

/// <summary>
/// 执行趋势项
/// </summary>
public class ExecutionTrendItem
{
    /// <summary>
    /// 日期
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 总执行次数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    public int Success { get; set; }

    /// <summary>
    /// 失败次数
    /// </summary>
    public int Failed { get; set; }
}
