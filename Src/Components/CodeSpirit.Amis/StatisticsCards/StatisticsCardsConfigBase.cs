namespace CodeSpirit.Amis.StatisticsCards;

/// <summary>
/// 统计卡片配置基类
/// </summary>
public abstract class StatisticsCardsConfigBase
{
    /// <summary>
    /// 配置统计卡片
    /// </summary>
    /// <param name="builder">统计卡片构建器</param>
    public abstract void Configure(StatisticsCardsBuilder builder);
    
    /// <summary>
    /// 获取构建后的配置
    /// </summary>
    /// <returns>统计卡片配置</returns>
    internal StatisticsCardsConfiguration GetConfiguration()
    {
        var builder = new StatisticsCardsBuilder();
        Configure(builder);
        return builder.Build();
    }
}

/// <summary>
/// 统计卡片配置（内部使用）
/// </summary>
internal class StatisticsCardsConfiguration
{
    /// <summary>
    /// API 路径
    /// </summary>
    public string Api { get; set; } = "statistics/cards";
    
    /// <summary>
    /// 自动刷新间隔（秒），0 表示不自动刷新
    /// </summary>
    public int RefreshInterval { get; set; } = 0;
    
    /// <summary>
    /// 每行卡片列数
    /// </summary>
    public int ColumnsCount { get; set; } = 4;
    
    /// <summary>
    /// 卡片间距（像素）
    /// </summary>
    public int Gap { get; set; } = 15;
    
    /// <summary>
    /// 卡片列表
    /// </summary>
    public List<CardDefinition> Cards { get; set; } = new();
}
