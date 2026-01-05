namespace CodeSpirit.Settings.Attributes;

/// <summary>
/// 设置 DTO 特性，用于标记设置数据传输对象，自动关联模块和配置键
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class SettingsDtoAttribute : Attribute
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Module { get; }
    
    /// <summary>
    /// 配置键名称
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// 初始化设置 DTO 特性
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">配置键名称</param>
    public SettingsDtoAttribute(string module, string key)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentException("模块名称不能为空", nameof(module));
        }
        
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("配置键名称不能为空", nameof(key));
        }
        
        Module = module;
        Key = key;
    }
}

