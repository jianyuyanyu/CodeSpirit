using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Core.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项查询 DTO
/// </summary>
public class ConfigItemQueryDto : QueryDtoBase
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [StringLength(36)]
    [DisplayName("应用")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用环境
    /// </summary>
    [DisplayName("环境")]
    public EnvironmentType? Environment { get; set; }

    /// <summary>
    /// 配置分组
    /// </summary>
    [StringLength(50)]
    [DisplayName("配置组")]
    public string Group { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [StringLength(100)]
    [DisplayName("配置键")]
    public string Key { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public ConfigStatus? Status { get; set; }

    /// <summary>
    /// 配置值类型
    /// </summary>
    [DisplayName("配置类型")]
    public ConfigValueType? ValueType { get; set; }

    /// <summary>
    /// 是否已发布上线
    /// </summary>
    [DisplayName("发布状态")]
    public bool? OnlineStatus { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("状态")]
    public bool? IsEnabled { get; set; }
} 