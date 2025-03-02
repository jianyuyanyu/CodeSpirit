namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 用于发布的配置项信息
/// </summary>
public class ConfigItemForPublishDto
{
    /// <summary>
    /// 配置项ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// 配置项值
    /// </summary>
    [Required]
    public string Value { get; set; }

    /// <summary>
    /// 配置项版本
    /// </summary>
    [Required]
    public long Version { get; set; }

    /// <summary>
    /// 配置项发布前的值
    /// </summary>
    public string OldValue { get; internal set; }
}