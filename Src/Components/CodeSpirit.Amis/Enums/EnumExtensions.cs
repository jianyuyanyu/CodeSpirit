namespace CodeSpirit.Amis.Enums;

/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 将 TabsMode 枚举转换为 AMIS 所需的字符串格式（小写）
    /// </summary>
    /// <param name="mode">Tabs模式</param>
    /// <returns>小写字符串</returns>
    public static string ToAmisString(this TabsMode mode)
    {
        return mode.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// 将 BadgeLevel 枚举转换为 AMIS 所需的字符串格式（小写）
    /// 用于生成 AMIS 文本颜色样式类名（如 text-info、text-warning）
    /// </summary>
    /// <param name="level">Badge级别</param>
    /// <returns>小写字符串，Default 返回空字符串</returns>
    public static string ToAmisString(this BadgeLevel level)
    {
        return level == BadgeLevel.Default ? "" : level.ToString().ToLowerInvariant();
    }
}

