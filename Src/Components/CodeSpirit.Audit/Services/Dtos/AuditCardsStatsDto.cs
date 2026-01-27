namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志统计卡片数据 DTO
/// </summary>
public class AuditCardsStatsDto
{
    /// <summary>
    /// 今日操作总数
    /// </summary>
    public long TodayTotal { get; set; }

    /// <summary>
    /// 今日成功操作数
    /// </summary>
    public long TodaySuccess { get; set; }

    /// <summary>
    /// 今日失败操作数
    /// </summary>
    public long TodayFailed { get; set; }

    /// <summary>
    /// 操作成功率（百分比，0-100）
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 今日活跃租户数（系统审计使用）
    /// </summary>
    public long TodayActiveTenants { get; set; }

    /// <summary>
    /// 今日活跃用户数（系统审计使用）
    /// </summary>
    public long TodayActiveUsers { get; set; }

    /// <summary>
    /// 最近7天总操作数（系统审计使用）
    /// </summary>
    public long Last7DaysTotal { get; set; }

    /// <summary>
    /// 平均响应时长（毫秒，系统审计使用）
    /// </summary>
    public double AvgResponseTime { get; set; }
}
