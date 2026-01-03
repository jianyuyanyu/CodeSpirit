namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 设置页面特性，标记控制器为设置页面
/// 将扫描控制器中所有带 HeaderOperationAttribute 的方法，每个方法对应一个Tab
/// 使用 HeaderOperationAttribute 的 Label 作为Tab标题，Icon 作为Tab图标
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SettingsPageAttribute : Attribute
{
    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// 页面描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Tab模式：line（横向）、card（卡片）、radio（单选）
    /// </summary>
    public string TabsMode { get; set; } = "line";
    
    /// <summary>
    /// 是否启用Tab切换动画
    /// </summary>
    public bool Animated { get; set; } = true;
    
    /// <summary>
    /// 初始化设置页面特性
    /// </summary>
    public SettingsPageAttribute() { }
    
    /// <summary>
    /// 初始化设置页面特性
    /// </summary>
    /// <param name="title">页面标题</param>
    public SettingsPageAttribute(string title)
    {
        Title = title;
    }
}

