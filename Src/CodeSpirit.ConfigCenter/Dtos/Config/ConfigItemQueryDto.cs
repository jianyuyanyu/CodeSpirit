using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项查询DTO
/// </summary>
public class ConfigItemQueryDto : QueryDtoBase
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [StringLength(36)]
    public string AppId { get; set; }

    /// <summary>
    /// 应用环境
    /// </summary>
    public EnvironmentType? Environment { get; set; }

    /// <summary>
    /// 配置分组
    /// </summary>
    [StringLength(50)]
    public string Group { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [StringLength(100)]
    public string Key { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public ConfigStatus? Status { get; set; }

    /// <summary>
    /// 配置值类型
    /// </summary>
    public ConfigValueType? ValueType { get; set; }

    /// <summary>
    /// 是否已发布上线
    /// </summary>
    public bool? OnlineStatus { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
} 