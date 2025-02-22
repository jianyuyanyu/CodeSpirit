using CodeSpirit.Amis.Attributes.Columns;

namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用 DTO
/// </summary>
public class AppDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string Id { get; set; }

    [DisplayName("应用名称")]
    [TplColumn(template: "${name}")]
    [Badge(VisibleOn = "!enabled", Level = "warning", Text = "已禁用")]
    public string Name { get; set; }

    [DisplayName("应用密钥")]
    [AmisColumn(Copyable = true)]
    public string Secret { get; set; }

    [DisplayName("描述")]
    public string Description { get; set; }

    [DisplayName("状态")]
    public bool Enabled { get; set; }

    [DisplayName("继承自")]
    [TplColumn(template: "${inheritancedAppId}")]
    public string InheritancedAppId { get; set; }

    [DisplayName("自动发布")]
    public bool AutoPublish { get; set; }

    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }
} 