namespace CodeSpirit.SurveyApi.Dtos.Survey;

/// <summary>
/// 问卷统计信息DTO
/// </summary>
[DisplayName("问卷统计")]
public class SurveyStatisticsDto
{
    /// <summary>
    /// 问卷ID
    /// </summary>
    [DisplayName("问卷ID")]
    public int SurveyId { get; set; }

    /// <summary>
    /// 问卷标题
    /// </summary>
    [DisplayName("问卷标题")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 总回答数
    /// </summary>
    [DisplayName("总回答数")]
    public int TotalResponses { get; set; }

    /// <summary>
    /// 完成回答数
    /// </summary>
    [DisplayName("完成回答数")]
    public int CompletedResponses { get; set; }

    /// <summary>
    /// 进行中回答数
    /// </summary>
    [DisplayName("进行中回答数")]
    public int InProgressResponses { get; set; }

    /// <summary>
    /// 放弃回答数
    /// </summary>
    [DisplayName("放弃回答数")]
    public int AbandonedResponses { get; set; }

    /// <summary>
    /// 完成率
    /// </summary>
    [DisplayName("完成率")]
    public decimal CompletionRate { get; set; }

    /// <summary>
    /// 平均完成时间（分钟）
    /// </summary>
    [DisplayName("平均完成时间")]
    public double AverageCompletionTimeMinutes { get; set; }

    /// <summary>
    /// 今日新增回答数
    /// </summary>
    [DisplayName("今日新增")]
    public int TodayNewResponses { get; set; }

    /// <summary>
    /// 本周新增回答数
    /// </summary>
    [DisplayName("本周新增")]
    public int WeekNewResponses { get; set; }

    /// <summary>
    /// 最近7天回答趋势
    /// </summary>
    [DisplayName("7天趋势")]
    public List<DailyResponseCount> Last7DaysTrend { get; set; } = new();

    /// <summary>
    /// 设备类型分布
    /// </summary>
    [DisplayName("设备分布")]
    public Dictionary<string, int> DeviceDistribution { get; set; } = new();

    /// <summary>
    /// 地域分布（基于IP）
    /// </summary>
    [DisplayName("地域分布")]
    public Dictionary<string, int> RegionDistribution { get; set; } = new();

    /// <summary>
    /// 草稿数量
    /// </summary>
    [DisplayName("草稿数量")]
    public int DraftCount { get; set; }

    /// <summary>
    /// 最后回答时间
    /// </summary>
    [DisplayName("最后回答时间")]
    public DateTime? LastResponseAt { get; set; }
}

/// <summary>
/// 每日回答数量
/// </summary>
public class DailyResponseCount
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 回答数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 完成数量
    /// </summary>
    public int CompletedCount { get; set; }
}
