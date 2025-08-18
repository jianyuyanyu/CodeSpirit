using CodeSpirit.ConfigCenter.Models.Enums;

namespace CodeSpirit.ConfigCenter.Dtos.PublishHistory;

/// <summary>
/// 配置项发布历史DTO
/// </summary>
public class ConfigItemPublishHistoryDto
{
    /// <summary>
    /// 配置项发布历史ID
    /// </summary>
    [DisplayName("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 配置发布历史ID
    /// </summary>
    [DisplayName("发布历史ID")]
    public int ConfigPublishHistoryId { get; set; }

    /// <summary>
    /// 配置项ID
    /// </summary>
    [DisplayName("配置项ID")]
    public int ConfigItemId { get; set; }

    /// <summary>
    /// 配置键
    /// </summary>
    [DisplayName("配置键")]
    public string Key { get; set; }

    /// <summary>
    /// 配置所属分组
    /// </summary>
    [DisplayName("分组")]
    public string Group { get; set; }

    /// <summary>
    /// 变更前的值
    /// </summary>
    [DisplayName("原值")]
    public string OldValue { get; set; }

    /// <summary>
    /// 变更后的值
    /// </summary>
    [DisplayName("新值")]
    public string NewValue { get; set; }

    /// <summary>
    /// 配置项版本
    /// </summary>
    [DisplayName("版本")]
    public int Version { get; set; }

    /// <summary>
    /// 配置值类型
    /// </summary>
    [DisplayName("值类型")]
    public ConfigValueType ValueType { get; set; }
}