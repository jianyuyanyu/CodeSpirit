namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 创建配置 DTO
/// </summary>
public class CreateConfigDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("应用")]
    [AmisSelectField(
        Source = "${API_HOST}/api/config/Apps",
        ValueField = "id",
        LabelField = "name",
        Searchable = true,
        Required = true,
        Placeholder = "请选择应用"
    )]
    public string AppId { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("配置键")]
    [RegularExpression(@"^[a-zA-Z0-9_:.]+$", ErrorMessage = "配置键只能包含字母、数字、下划线、冒号和点")]
    public string Key { get; set; }

    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    [DisplayName("配置值")]
    //[AmisEditorField(
    //    Language = "json",
    //    Size = "lg",
    //    AllowFullscreen = true,
    //    VisibleOn = "type === 'json'"
    //)]
    //[AmisTextareaField(
    //    ShowCounter = true,
    //    VisibleOn = "type === 'text'"
    //)]
    public string Value { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [Required]
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 配置组
    /// </summary>
    [DisplayName("分组")]
    [AmisSelectField(
        Source = "${API_HOST}/api/config/Groups",
        ValueField = "name",
        LabelField = "name",
        Searchable = true,
        Clearable = true
    )]
    public string Group { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [DisplayName("类型")]
    public string Type { get; set; }
} 