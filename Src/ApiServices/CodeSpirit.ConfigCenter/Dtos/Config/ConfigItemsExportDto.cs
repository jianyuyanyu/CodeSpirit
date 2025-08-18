using CodeSpirit.Amis.Attributes.FormFields;
using System.ComponentModel;

public class ConfigItemsExportDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 配置项集合，Key为配置键，Value为配置值
    /// </summary>
    [DisplayName("配置项集合")]
    [AmisFormField(type: "json")]
    public Dictionary<string, object> Configs { get; set; }

    /// <summary>
    /// 是否包含继承的配置
    /// </summary>
    [DisplayName("包含继承配置")]
    public bool IncludesInheritedConfig { get; set; }
}