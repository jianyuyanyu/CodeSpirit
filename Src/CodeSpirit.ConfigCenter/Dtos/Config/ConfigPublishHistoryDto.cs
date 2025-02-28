using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.ConfigCenter.Dtos.Config;

/// <summary>
/// 配置发布历史DTO
/// </summary>
public class ConfigPublishHistoryDto
{
    /// <summary>
    /// 发布历史ID
    /// </summary>
    [DisplayName("ID")]
    public int Id { get; set; }

    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    [DisplayName("应用名称")]
    public string AppName { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [DisplayName("发布说明")]
    public string Description { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [DisplayName("版本")]
    public long Version { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [DisplayName("发布时间")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 发布人
    /// </summary>
    [DisplayName("发布人")]
    public string CreatedBy { get; set; }

    /// <summary>
    /// 配置项发布历史列表
    /// </summary>
    [DisplayName("配置项变更")]
    public List<ConfigItemPublishHistoryDto> ConfigItemPublishHistories { get; set; } = new();
}
