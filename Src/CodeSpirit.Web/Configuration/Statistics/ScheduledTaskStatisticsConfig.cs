using CodeSpirit.Amis.StatisticsCards;

namespace CodeSpirit.Web.Configuration.Statistics;

/// <summary>
/// 定时任务统计卡片配置
/// </summary>
public class ScheduledTaskStatisticsConfig : StatisticsCardsConfigBase
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
            .AddCard("todayExecutions", "今日执行")
                .WithIcon("fa-play-circle")
                .WithColor(CardColor.Info)
            .AddCard("todaySuccessExecutions", "今日成功")
                .WithIcon("fa-check-circle")
                .WithColor(CardColor.Success)
            .AddCard("todayFailedExecutions", "今日失败")
                .WithIcon("fa-times-circle")
                .WithColor(CardColor.Danger)
            .AddCard("successRate", "成功率")
                .WithIcon("fa-chart-line")
                .WithColor(CardColor.Warning);
    }
}
