/// <summary>
/// 更新配置 DTO
/// </summary>
public class UpdateConfigDto
{
    /// <summary>
    /// 配置值
    /// </summary>
    [Required]
    public string Value { get; set; }

    /// <summary>
    /// 配置组
    /// </summary>
    [StringLength(100)]
    public string Group { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }

    /// <summary>
    /// 配置类型
    /// </summary>
    [StringLength(50)]
    public string Type { get; set; }
} 