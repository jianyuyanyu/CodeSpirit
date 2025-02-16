using CodeSpirit.Core.Dtos;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.QueryDtos;

/// <summary>
/// 配置查询参数
/// </summary>
public class ConfigQueryDto : QueryDtoBase
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    public string Environment { get; set; }

    /// <summary>
    /// 配置组
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// 发布状态
    /// </summary>
    public string Status { get; set; }
} 