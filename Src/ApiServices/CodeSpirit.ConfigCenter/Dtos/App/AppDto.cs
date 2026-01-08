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
    [DisplayName("应用ID")]
    [AmisColumn(Copyable = true)]
    public string Id { get; set; }

    [DisplayName("应用ID")]
    [AmisColumn(Copyable = true)]
    public string AppId => Id;

    /// <summary>
    /// 应用名称
    /// </summary>
    [DisplayName("应用名称")]
    [TplColumn(template: "${name}")]
    [Badge(VisibleOn = "healthStatus", Level = "success", Position = "top-right", Animation = true, OffsetX = 10)]
    public string Name { get; set; }

    /// <summary>
    /// 应用标签列表（用于显示）
    /// </summary>
    [DisplayName("标签")]
    [TagsColumn(Color = "info")]
    public List<string> Tags { get; set; }

    /// <summary>
    /// 启用状态
    /// </summary>
    [DisplayName("启用状态")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public bool Enabled { get; set; }

    /// <summary>
    /// 服务健康状态
    /// true表示健康，false表示不健康，null表示未知
    /// </summary>
    [DisplayName("健康状态")]
    [AmisColumn(QuickEdit = false, Disabled = true, Hidden = true)]
    public bool? HealthStatus { get; set; }

    /// <summary>
    /// 当前配置版本号（从发布历史获取，不存储在数据库中）
    /// </summary>
    [DisplayName("配置版本")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public long ConfigVersion { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    [DisplayName("描述")]
    public string Description { get; set; }

    /// <summary>
    /// 继承应用名称
    /// </summary>
    [DisplayName("继承自")]
    public string InheritancedAppName { get; set; }

    /// <summary>
    /// 是否自动发布
    /// </summary>
    [DisplayName("自动发布")]
    public bool AutoPublish { get; set; }

    /// <summary>
    /// 是否为自动注册的应用
    /// </summary>
    [DisplayName("自动注册")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public bool IsAutoRegistered { get; set; }

    /// <summary>
    /// 应用密钥（仅在详情页显示）
    /// </summary>
    [DisplayName("应用密钥")]
    [AmisColumn(Copyable = true, Hidden = true)]
    public string Secret { get; set; }

    /// <summary>
    /// 继承应用ID（内部使用）
    /// </summary>
    [IgnoreColumn]
    public string InheritancedAppId { get; set; }

    /// <summary>
    /// 配置数量
    /// </summary>
    [DisplayName("配置数")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public int ConfigCount { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }
}