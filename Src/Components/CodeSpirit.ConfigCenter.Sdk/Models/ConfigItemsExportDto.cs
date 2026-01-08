namespace CodeSpirit.ConfigCenter.Sdk.Models;

/// <summary>
/// 配置项导出 DTO
/// </summary>
public class ConfigItemsExportDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 配置项集合，Key为配置键，Value为配置值
    /// </summary>
    public Dictionary<string, object> Configs { get; set; } = new();

    /// <summary>
    /// 是否包含继承的配置
    /// </summary>
    public bool IncludesInheritedConfig { get; set; }
}

