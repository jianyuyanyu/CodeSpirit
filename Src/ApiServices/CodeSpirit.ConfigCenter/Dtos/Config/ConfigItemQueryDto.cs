using CodeSpirit.Amis.Attributes;
using CodeSpirit.Amis.Attributes.FormFields;
using CodeSpirit.ConfigCenter.Configuration;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置项查询 DTO
/// </summary>
[PageTabs<ConfigItemTabsConfig>]
public class ConfigItemQueryDto : QueryDtoBase
{
    /// <summary>
    /// 所属应用ID
    /// </summary>
    [StringLength(36)]
    [DisplayName("应用")]
    [AmisSelectField(
        Source = "${ROOT_API}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择应用"
    )]
    public string AppId { get; set; }

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
    [DisplayName("配置状态")]
    public ConfigStatus? Status { get; set; }

    /// <summary>
    /// 配置值类型
    /// </summary>
    [DisplayName("配置类型")]
    public ConfigValueType? ValueType { get; set; }
}