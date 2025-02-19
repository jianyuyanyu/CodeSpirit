using CodeSpirit.Amis.Attributes.FormFields;

/// <summary>
/// 更新应用 DTO
/// </summary>
public class UpdateAppDto
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("应用名称")]
    public string Name { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    [DisplayName("描述")]
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool Enabled { get; set; }

    /// <summary>
    /// 继承的应用ID
    /// </summary>
    [DisplayName("继承自")]
    [StringLength(100)]
    [AmisSelectField(
        Source = "${ROOT_API}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Multiple = false,
        Clearable = true,
        Placeholder = "请选择要继承的应用"
    )]
    public string InheritancedAppId { get; set; }

    /// <summary>
    /// 是否自动发布
    /// </summary>
    [DisplayName("自动发布")]
    [Description("开启后，配置变更将自动发布")]
    public bool? AutoPublish { get; set; }
}