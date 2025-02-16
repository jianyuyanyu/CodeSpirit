using Newtonsoft.Json;

namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用批量导入项数据传输对象
/// </summary>
public class AppBatchImportItemDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [JsonProperty("应用ID")]
    [Required]
    [MaxLength(100)]
    public string Id { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    [JsonProperty("名称")]
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    [JsonProperty("描述")]
    [MaxLength(256)]
    public string Description { get; set; }

    /// <summary>
    /// 是否自动发布配置
    /// </summary>
    [JsonProperty("自动发布")]
    public bool AutoPublish { get; set; }
} 