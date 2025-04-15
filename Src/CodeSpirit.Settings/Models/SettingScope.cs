namespace CodeSpirit.Settings.Models;

/// <summary>
/// 设置范围
/// </summary>
public enum SettingScope
{
    /// <summary>
    /// 全局设置
    /// </summary>
    Global = 0,
    
    /// <summary>
    /// 用户设置
    /// </summary>
    User = 1,
    
    /// <summary>
    /// 模块设置
    /// </summary>
    Module = 2,
    
    /// <summary>
    /// 组织设置
    /// </summary>
    Organization = 3,
    
    /// <summary>
    /// 角色设置
    /// </summary>
    Role = 4
} 