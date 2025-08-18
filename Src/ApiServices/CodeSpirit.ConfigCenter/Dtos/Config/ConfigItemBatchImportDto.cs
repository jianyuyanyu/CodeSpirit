using CodeSpirit.ConfigCenter.Models.Enums;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项批量导入数据传输对象
/// </summary>
public class ConfigItemBatchImportDto
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [JsonProperty("应用ID")]
    [Required]
    [StringLength(36)]
    public required string AppId { get; set; }

    /// <summary>
    /// 配置键名
    /// </summary>
    [JsonProperty("配置键")]
    [Required]
    [StringLength(100)]
    public required string Key { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [JsonProperty("配置值")]
    [Required]
    [StringLength(4000)]
    public required string Value { get; set; }

    /// <summary>
    /// 应用环境，如Development/Staging/Production
    /// </summary>
    [JsonProperty("应用环境")]
    [Required]
    public required EnvironmentType Environment { get; set; } = EnvironmentType.Development;

    /// <summary>
    /// 配置分组
    /// </summary>
    [JsonProperty("配置组")]
    [StringLength(50)]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// 配置说明
    /// </summary>
    [JsonProperty("配置描述")]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 配置值类型（如：text/json/yaml等）
    /// </summary>
    [JsonProperty("配置类型")]
    [Required]
    public ConfigValueType ValueType { get; set; } = ConfigValueType.String;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonProperty("是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 配置状态
    /// </summary>
    [JsonProperty("配置状态")]
    public ConfigStatus Status { get; set; } = ConfigStatus.Init;
} 