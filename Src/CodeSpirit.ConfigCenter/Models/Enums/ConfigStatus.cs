using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Models.Enums;

/// <summary>
/// 配置项状态
/// </summary>
public enum ConfigStatus
{
    /// <summary>
    /// 初始状态
    /// </summary>
    [Display(Name = "初始状态")]
    Init = 0,
    
    /// <summary>
    /// 编辑中
    /// </summary>
    [Display(Name = "编辑中")]
    Editing = 1,
    
    /// <summary>
    /// 已发布
    /// </summary>
    [Display(Name = "已发布")]
    Released = 2
} 