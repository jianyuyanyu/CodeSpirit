using CodeSpirit.Amis.StatisticsCards;

namespace CodeSpirit.Web.Configuration.Statistics;

/// <summary>
/// 系统审计日志统计卡片配置
/// </summary>
public class SystemAuditLogStatisticsConfig : StatisticsCardsConfigBase
{
    /// <summary>
    /// 配置统计卡片
    /// </summary>
    /// <param name="builder">统计卡片构建器</param>
    public override void Configure(StatisticsCardsBuilder builder)
    {
        builder
            .SetApi("statistics/cards")
            .SetRefreshInterval(60)
            .SetColumnsCount(3)
            .SetGap(15)
            .AddCard("todayTotal", "今日操作")
                .WithIcon("fa-list")
                .WithColor(CardColor.Info)
            .AddCard("todayActiveTenants", "今日活跃租户")
                .WithIcon("fa-building")
                .WithColor(CardColor.Primary)
            .AddCard("todayActiveUsers", "今日活跃用户")
                .WithIcon("fa-users")
                .WithColor(CardColor.Success)
            .AddCard("successRate", "操作成功率")
                .WithIcon("fa-chart-line")
                .WithColor(CardColor.Warning)
            .AddCard("last7DaysTotal", "近7天操作数")
                .WithIcon("fa-calendar")
                .WithColor(CardColor.Secondary)
            .AddCard("avgResponseTime", "平均响应时长")
                .WithIcon("fa-bolt")
                .WithColor(CardColor.Info);
    }
}
