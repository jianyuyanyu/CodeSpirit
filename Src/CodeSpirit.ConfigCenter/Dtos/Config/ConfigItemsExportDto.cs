public class ConfigItemsExportDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    public string Environment { get; set; }

    /// <summary>
    /// 配置项集合，Key为配置键，Value为配置值
    /// </summary>
    public Dictionary<string, string> Configs { get; set; }
} 