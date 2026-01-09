namespace CodeSpirit.Amis.StatisticsCards;

/// <summary>
/// 统计卡片构建器
/// </summary>
public class StatisticsCardsBuilder
{
    private readonly StatisticsCardsConfiguration _config = new();
    private CardDefinition? _currentCard;
    
    /// <summary>
    /// 设置 API 路径
    /// </summary>
    /// <param name="api">API 相对路径，如 "statistics/cards"</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder SetApi(string api)
    {
        _config.Api = api;
        return this;
    }
    
    /// <summary>
    /// 设置自动刷新间隔（秒）
    /// </summary>
    /// <param name="seconds">刷新间隔（秒），0 表示不自动刷新</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder SetRefreshInterval(int seconds)
    {
        _config.RefreshInterval = seconds;
        return this;
    }
    
    /// <summary>
    /// 设置每行卡片列数
    /// </summary>
    /// <param name="count">列数，默认 4</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder SetColumnsCount(int count)
    {
        _config.ColumnsCount = count;
        return this;
    }
    
    /// <summary>
    /// 设置卡片间距（像素）
    /// </summary>
    /// <param name="gap">间距（像素），默认 15</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder SetGap(int gap)
    {
        _config.Gap = gap;
        return this;
    }
    
    /// <summary>
    /// 添加卡片
    /// </summary>
    /// <param name="field">数据字段名</param>
    /// <param name="title">显示标题</param>
    /// <returns>构建器实例，可继续调用 WithIcon、WithColor 等方法</returns>
    public StatisticsCardsBuilder AddCard(string field, string title)
    {
        _currentCard = new CardDefinition
        {
            Field = field,
            Title = title
        };
        _config.Cards.Add(_currentCard);
        return this;
    }
    
    /// <summary>
    /// 设置当前卡片图标
    /// </summary>
    /// <param name="icon">FontAwesome 图标类名，如 "fa-play-circle"</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder WithIcon(string icon)
    {
        if (_currentCard != null)
            _currentCard.Icon = icon;
        return this;
    }
    
    /// <summary>
    /// 设置当前卡片颜色
    /// </summary>
    /// <param name="color">卡片颜色枚举</param>
    /// <returns>构建器实例</returns>
    public StatisticsCardsBuilder WithColor(CardColor color)
    {
        if (_currentCard != null)
            _currentCard.Color = color.ToString().ToLower();
        return this;
    }
    
    /// <summary>
    /// 构建配置
    /// </summary>
    /// <returns>统计卡片配置</returns>
    internal StatisticsCardsConfiguration Build() => _config;
}
