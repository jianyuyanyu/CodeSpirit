namespace CodeSpirit.Settings.Models;

/// <summary>
/// 设置值类型
/// </summary>
public enum SettingValueType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String = 0,
    
    /// <summary>
    /// 整数
    /// </summary>
    Integer = 1,
    
    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean = 2,
    
    /// <summary>
    /// 小数
    /// </summary>
    Decimal = 3,
    
    /// <summary>
    /// 日期时间
    /// </summary>
    DateTime = 4,
    
    /// <summary>
    /// JSON
    /// </summary>
    Json = 5,
    
    /// <summary>
    /// 单选
    /// </summary>
    Select = 6,
    
    /// <summary>
    /// 多选
    /// </summary>
    MultiSelect = 7,
    
    /// <summary>
    /// 密码
    /// </summary>
    Password = 8,
    
    /// <summary>
    /// 富文本
    /// </summary>
    RichText = 9,
    
    /// <summary>
    /// 颜色
    /// </summary>
    Color = 10
} 