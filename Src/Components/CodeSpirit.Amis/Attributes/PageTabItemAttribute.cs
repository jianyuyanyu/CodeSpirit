using CodeSpirit.Amis.Enums;

namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 页面Tab项配置特性，用于定义单个Tab的配置
/// 在查询DTO类上使用此特性（AllowMultiple），每个特性对应一个Tab
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PageTabItemAttribute : Attribute
{
    /// <summary>
    /// Tab唯一标识（用于defaultKey和badge变量名）
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Tab显示标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 过滤条件JSON字符串，如：{"status": 1} 或 {"lowStock": true}
    /// 该条件会在Tab切换时应用到CRUD的filter中
    /// </summary>
    public string Filter { get; set; } = "";

    /// <summary>
    /// Tab排序顺序，数字越小越靠前
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Tab图标（可选），如：fa-solid fa-box
    /// </summary>
    public string Icon { get; set; } = "";

    /// <summary>
    /// Badge样式级别
    /// 默认为 Default（使用默认样式）
    /// </summary>
    public BadgeLevel BadgeLevel { get; set; } = BadgeLevel.Default;
}

