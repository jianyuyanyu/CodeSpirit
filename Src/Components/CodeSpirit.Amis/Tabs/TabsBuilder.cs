using System.Linq.Expressions;
using CodeSpirit.Amis.Enums;

namespace CodeSpirit.Amis.Tabs;

/// <summary>
/// Tabs流式构建器
/// </summary>
/// <typeparam name="TQueryDto">查询DTO类型</typeparam>
public class TabsBuilder<TQueryDto> where TQueryDto : class, new()
{
    private readonly TabsConfiguration<TQueryDto> _configuration = new();
    private TabItemConfig<TQueryDto>? _currentTab;

    /// <summary>
    /// 设置获取各Tab数量的API路径
    /// </summary>
    public TabsBuilder<TQueryDto> SetCountApi(string countApi)
    {
        _configuration.CountApi = countApi;
        return this;
    }

    /// <summary>
    /// 设置Tab样式模式
    /// </summary>
    /// <param name="tabsMode">Tabs模式枚举</param>
    public TabsBuilder<TQueryDto> SetTabsMode(TabsMode tabsMode)
    {
        _configuration.TabsMode = tabsMode.ToAmisString();
        return this;
    }

    /// <summary>
    /// 设置Tab样式模式（字符串版本，向后兼容）
    /// </summary>
    /// <param name="tabsMode">line/card/radio</param>
    [Obsolete("请使用 SetTabsMode(TabsMode) 枚举版本")]
    public TabsBuilder<TQueryDto> SetTabsMode(string tabsMode)
    {
        _configuration.TabsMode = tabsMode;
        return this;
    }

    /// <summary>
    /// 设置默认选中的Tab
    /// </summary>
    public TabsBuilder<TQueryDto> SetDefaultTab(string defaultTab)
    {
        _configuration.DefaultTab = defaultTab;
        return this;
    }

    /// <summary>
    /// 设置是否显示数量badge
    /// </summary>
    public TabsBuilder<TQueryDto> SetShowBadge(bool showBadge)
    {
        _configuration.ShowBadge = showBadge;
        return this;
    }

    /// <summary>
    /// 添加Tab项
    /// </summary>
    /// <param name="key">Tab唯一标识</param>
    /// <param name="title">Tab显示标题</param>
    public TabsBuilder<TQueryDto> AddTab(string key, string title)
    {
        _currentTab = new TabItemConfig<TQueryDto>
        {
            Key = key,
            Title = title
        };
        _configuration.TabItems.Add(_currentTab);
        return this;
    }

    /// <summary>
    /// 为当前Tab设置过滤条件
    /// </summary>
    /// <param name="filterAction">过滤条件设置动作</param>
    public TabsBuilder<TQueryDto> WithFilter(Action<TQueryDto> filterAction)
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.FilterAction = filterAction;
        return this;
    }

    /// <summary>
    /// 为当前Tab设置排序顺序
    /// </summary>
    public TabsBuilder<TQueryDto> WithOrder(int order)
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.Order = order;
        return this;
    }

    /// <summary>
    /// 为当前Tab设置图标
    /// </summary>
    public TabsBuilder<TQueryDto> WithIcon(string icon)
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.Icon = icon;
        return this;
    }

    /// <summary>
    /// 为当前Tab设置Badge样式级别
    /// </summary>
    /// <param name="badgeLevel">Badge级别枚举</param>
    public TabsBuilder<TQueryDto> WithBadgeLevel(BadgeLevel badgeLevel)
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.BadgeLevel = badgeLevel.ToAmisString();
        return this;
    }

    /// <summary>
    /// 为当前Tab设置Badge样式级别（字符串版本，向后兼容）
    /// </summary>
    /// <param name="badgeLevel">info/success/warning/danger</param>
    [Obsolete("请使用 WithBadgeLevel(BadgeLevel) 枚举版本")]
    public TabsBuilder<TQueryDto> WithBadgeLevel(string badgeLevel)
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.BadgeLevel = badgeLevel;
        return this;
    }

    /// <summary>
    /// 为当前Tab设置自定义统计方法
    /// </summary>
    /// <param name="countMethod">自定义统计方法</param>
    public TabsBuilder<TQueryDto> WithCustomCount<TEntity>(Func<IQueryable<TEntity>, Task<int>> countMethod)
        where TEntity : class
    {
        if (_currentTab == null)
        {
            throw new InvalidOperationException("必须先调用 AddTab 方法");
        }

        _currentTab.CustomCountMethod = async query => await countMethod((IQueryable<TEntity>)query);
        return this;
    }

    /// <summary>
    /// 构建配置
    /// </summary>
    internal TabsConfiguration<TQueryDto> Build()
    {
        // 按Order排序
        _configuration.TabItems = _configuration.TabItems
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Key)
            .ToList();

        return _configuration;
    }
}

