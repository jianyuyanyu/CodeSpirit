namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 页面侧边栏配置特性，用于标记需要在侧边栏显示的表单字段
/// 在查询DTO的属性上使用此特性，该字段将被包含在页面的aside区域中
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class PageAsideAttribute : Attribute
{
    /// <summary>
    /// 表单提交目标。如果为空，则自动设置为CRUD组件名称；如果不为空，则使用指定值
    /// </summary>
    public string Target { get; set; } = "";
    
    /// <summary>
    /// 是否在初始化时提交
    /// </summary>
    public bool SubmitOnInit { get; set; } = false;
    
    /// <summary>
    /// 是否不使用面板包装，默认为false
    /// </summary>
    public bool WrapWithPanel { get; set; } = false;
}
