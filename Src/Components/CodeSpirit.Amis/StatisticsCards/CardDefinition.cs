namespace CodeSpirit.Amis.StatisticsCards;

/// <summary>
/// 卡片定义
/// </summary>
public class CardDefinition
{
    /// <summary>
    /// 数据字段名
    /// </summary>
    public string Field { get; set; } = "";
    
    /// <summary>
    /// 显示标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 图标（FontAwesome）
    /// </summary>
    public string Icon { get; set; } = "";
    
    /// <summary>
    /// 颜色主题
    /// </summary>
    public string Color { get; set; } = "info";
}
