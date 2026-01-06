using System.Linq.Expressions;

namespace CodeSpirit.Amis.Tabs;

/// <summary>
/// Tabs配置基类，用于定义页面顶部Tab的强类型配置
/// </summary>
/// <typeparam name="TQueryDto">查询DTO类型</typeparam>
public abstract class TabsConfigBase<TQueryDto> where TQueryDto : class, new()
{
    /// <summary>
    /// 获取Tabs构建器
    /// </summary>
    protected TabsBuilder<TQueryDto> Builder { get; } = new TabsBuilder<TQueryDto>();

    /// <summary>
    /// 配置Tabs
    /// </summary>
    /// <param name="builder">Tabs构建器</param>
    public abstract void Configure(TabsBuilder<TQueryDto> builder);

    /// <summary>
    /// 获取构建后的Tabs配置
    /// </summary>
    internal TabsConfiguration<TQueryDto> GetConfiguration()
    {
        Configure(Builder);
        return Builder.Build();
    }
}

/// <summary>
/// Tabs配置（内部使用）
/// </summary>
internal class TabsConfiguration<TQueryDto> where TQueryDto : class, new()
{
    /// <summary>
    /// 获取各Tab数量的API路径
    /// </summary>
    public string CountApi { get; set; } = "";

    /// <summary>
    /// Tab样式模式
    /// </summary>
    public string TabsMode { get; set; } = "line";

    /// <summary>
    /// 默认选中的Tab key
    /// </summary>
    public string DefaultTab { get; set; } = "";

    /// <summary>
    /// 是否显示数量badge
    /// </summary>
    public bool ShowBadge { get; set; } = true;

    /// <summary>
    /// Tab项列表
    /// </summary>
    public List<TabItemConfig<TQueryDto>> TabItems { get; set; } = new();
}

