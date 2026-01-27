using CodeSpirit.Amis.StatisticsCards;

namespace CodeSpirit.Web.Configuration.Statistics;

/// <summary>
/// 租户审计日志统计卡片配置
/// </summary>
public class AuditLogStatisticsConfig : StatisticsCardsConfigBase
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
            .SetColumnsCount(4)
            .SetGap(15)
            .AddCard("todayTotal", "今日操作")
                .WithIcon("fa-list")
                .WithColor(CardColor.Info)
            .AddCard("todaySuccess", "今日成功")
                .WithIcon("fa-check-circle")
                .WithColor(CardColor.Success)
            .AddCard("todayFailed", "今日失败")
                .WithIcon("fa-times-circle")
                .WithColor(CardColor.Danger)
            .AddCard("successRate", "操作成功率")
                .WithIcon("fa-chart-line")
                .WithColor(CardColor.Warning);
    }
}
