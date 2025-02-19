using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Models.Enums;

/// <summary>
/// 配置值类型
/// </summary>
public enum ConfigValueType
{
    /// <summary>
    /// 字符串
    /// </summary>
    [Display(Name = "字符串")]
    String,
    
    /// <summary>
    /// 整数
    /// </summary>
    [Display(Name = "整数")]
    Int,
    
    /// <summary>
    /// 浮点数
    /// </summary>
    [Display(Name = "浮点数")]
    Double,
    
    /// <summary>
    /// 布尔值
    /// </summary>
    [Display(Name = "布尔值")]
    Boolean,
    
    /// <summary>
    /// JSON
    /// </summary>
    [Display(Name = "JSON")]
    Json,
    
    /// <summary>
    /// 密文
    /// </summary>
    [Display(Name = "密文")]
    Encrypted
} 