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
    public string Name { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 继承的应用ID
    /// </summary>
    [StringLength(100)]
    public string InheritancedAppId { get; set; }

    /// <summary>
    /// 是否自动发布
    /// </summary>
    public bool AutoPublish { get; set; }
}