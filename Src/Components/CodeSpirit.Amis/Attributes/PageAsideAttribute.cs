using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Amis.Attributes;

/// <summary>
/// 页面侧边栏位置枚举
/// </summary>
public enum AsidePosition
{
    /// <summary>
    /// 左侧
    /// </summary>
    [Display(Name = "左侧")]
    Left = 0,
    
    /// <summary>
    /// 右侧
    /// </summary>
    [Display(Name = "右侧")]
    Right = 1
}

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
    
    /// <summary>
    /// 页面的边栏区域宽度是否可调整，默认为true
    /// </summary>
    public bool AsideResizor { get; set; } = true;
    
    /// <summary>
    /// 页面边栏区域的最小宽度(像素)，默认为0表示未设置
    /// </summary>
    public int AsideMinWidth { get; set; } = 0;
    
    /// <summary>
    /// 页面边栏区域的最大宽度(像素)，默认为0表示未设置
    /// </summary>
    public int AsideMaxWidth { get; set; } = 0;
    
    /// <summary>
    /// 用来控制边栏固定与否，默认为true
    /// </summary>
    public bool AsideSticky { get; set; } = true;
    
    /// <summary>
    /// 页面边栏区域的位置，默认为左侧
    /// </summary>
    public AsidePosition AsidePosition { get; set; } = AsidePosition.Left;
}
