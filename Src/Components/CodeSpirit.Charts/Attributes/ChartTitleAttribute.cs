namespace CodeSpirit.Charts.Attributes;

/// <summary>
/// 图表标题特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ChartTitleAttribute : Attribute
{
    /// <summary>
    /// 图表标题
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 初始化图表标题特性
    /// </summary>
    /// <param name="title">图表标题</param>
    public ChartTitleAttribute(string title)
    {
        Title = title;
    }
}