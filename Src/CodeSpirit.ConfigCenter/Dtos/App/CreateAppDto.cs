namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 创建应用 DTO
/// </summary>
public class CreateAppDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("应用ID")]
    [Description("应用ID只能包含字母、数字和下划线")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "应用ID只能包含字母、数字和下划线")]
    public string Id { get; set; }

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
    //[AmisTextareaField(MaxLength = 500, ShowCounter = true)]
    public string Description { get; set; }

    /// <summary>
    /// 继承的应用ID
    /// </summary>
    [DisplayName("继承自")]
    [StringLength(100)]
    [AmisSelectField(
        Source = "${API_HOST}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Clearable = true,
        Placeholder = "请选择要继承的应用"
    )]
    public string InheritancedAppId { get; set; }

    /// <summary>
    /// 是否自动发布
    /// </summary>
    [DisplayName("自动发布")]
    [Description("开启后，配置变更将自动发布")]
    public bool AutoPublish { get; set; }
} 