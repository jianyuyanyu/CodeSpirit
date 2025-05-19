namespace CodeSpirit.Charts.Attributes;

/// <summary>
/// 图表描述特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ChartDescriptionAttribute : Attribute
{
    /// <summary>
    /// 图表描述
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 初始化图表描述特性
    /// </summary>
    /// <param name="description">图表描述</param>
    public ChartDescriptionAttribute(string description)
    {
        Description = description;
    }
}