using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Models.Enums;

/// <summary>
/// 应用环境类型
/// </summary>
public enum EnvironmentType
{
    /// <summary>
    /// 开发环境
    /// </summary>
    [Display(Name = "开发环境")]
    Development = 0,

    /// <summary>
    /// 预发布环境
    /// </summary>
    [Display(Name = "预发布环境")]
    Staging = 1,

    /// <summary>
    /// 生产环境
    /// </summary>
    [Display(Name = "生产环境")]
    Production = 2
}