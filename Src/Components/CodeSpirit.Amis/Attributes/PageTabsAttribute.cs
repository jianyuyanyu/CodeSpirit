using CodeSpirit.Amis.Enums;

namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 页面顶部Tab配置特性，用于标记查询DTO需要生成顶部Tab切换
/// 在查询DTO类上使用此特性，将生成包含多个Tab的页面结构
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PageTabsAttribute : Attribute
{
    /// <summary>
    /// 获取各Tab数量的API路径（用于显示badge数字）
    /// 该API应返回包含各Tab数量的JSON对象，如：{"onSaleCount": 10, "offSaleCount": 5}
    /// </summary>
    public string CountApi { get; set; } = "";

    /// <summary>
    /// Tab样式模式
    /// 默认为 Line（横向）
    /// </summary>
    public TabsMode TabsMode { get; set; } = TabsMode.Line;

    /// <summary>
    /// 默认选中的Tab key
    /// </summary>
    public string DefaultTab { get; set; } = "";

    /// <summary>
    /// 是否显示数量badge，默认为true
    /// </summary>
    public bool ShowBadge { get; set; } = true;

    /// <summary>
    /// Tabs配置类类型（用于强类型配置）
    /// </summary>
    public Type? ConfigType { get; set; }
}

/// <summary>
/// 页面顶部Tab配置特性（泛型版本），使用强类型配置类
/// </summary>
/// <typeparam name="TConfig">Tabs配置类类型，必须继承自 TabsConfigBase</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PageTabsAttribute<TConfig> : PageTabsAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PageTabsAttribute()
    {
        ConfigType = typeof(TConfig);
    }
}
