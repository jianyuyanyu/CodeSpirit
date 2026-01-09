using CodeSpirit.Amis.StatisticsCards;

namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 统计卡片特性（泛型版本），使用强类型配置类
/// </summary>
/// <typeparam name="TConfig">配置类类型，必须继承自 StatisticsCardsConfigBase</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public class StatisticsCardsAttribute<TConfig> : Attribute 
    where TConfig : StatisticsCardsConfigBase, new()
{
    /// <summary>
    /// 配置类类型
    /// </summary>
    public Type ConfigType { get; } = typeof(TConfig);
}
