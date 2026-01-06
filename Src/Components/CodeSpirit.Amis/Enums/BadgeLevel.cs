namespace CodeSpirit.Amis.Enums;

/// <summary>
/// Badge样式级别，对应 AMIS 文本颜色样式类
/// 参考：https://aisuda.bce.baidu.com/amis/zh-CN/style/typography/text-color
/// </summary>
public enum BadgeLevel
{
    /// <summary>
    /// 默认样式（静音灰色）
    /// 对应 AMIS 样式类：text-muted
    /// </summary>
    Default,

    /// <summary>
    /// 信息样式（信息蓝色）
    /// 对应 AMIS 样式类：text-info
    /// </summary>
    Info,

    /// <summary>
    /// 成功样式（成功绿色）
    /// 对应 AMIS 样式类：text-success
    /// </summary>
    Success,

    /// <summary>
    /// 警告样式（警告橙色）
    /// 对应 AMIS 样式类：text-warning
    /// </summary>
    Warning,

    /// <summary>
    /// 危险样式（危险红色）
    /// 对应 AMIS 样式类：text-danger
    /// </summary>
    Danger
}

