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

    /// <summary>
    /// 服务健康状态
    /// true表示健康，false表示不健康，null表示未知
    /// </summary>
    [DisplayName("健康状态")]
    [TplColumn(template: "${healthStatus === true ? '<span class=\"antd-badge antd-badge-status antd-badge-status-success\"><span class=\"antd-badge-status-dot\"></span>健康</span>' : healthStatus === false ? '<span class=\"antd-badge antd-badge-status antd-badge-status-error\"><span class=\"antd-badge-status-dot\"></span>异常</span>' : '<span class=\"antd-badge antd-badge-status antd-badge-status-warning\"><span class=\"antd-badge-status-dot\"></span>未知</span>'}")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public bool? HealthStatus { get; set; }

    [DisplayName("应用密钥")]
    [AmisColumn(Copyable = true)]
    public string Secret { get; set; }

    [DisplayName("描述")]
    public string Description { get; set; }

    [DisplayName("启用状态")]
    public bool Enabled { get; set; }

    [IgnoreColumn]
    public string InheritancedAppId { get; set; }

    /// <summary>
    /// 继承应用名称
    /// </summary>
    [DisplayName("继承自")]
    public string InheritancedAppName { get; set; }

    /// <summary>
    /// 是否为自动注册的应用
    /// </summary>
    [DisplayName("自动注册")]
    [AmisColumn(QuickEdit = false, Disabled = true)]
    public bool IsAutoRegistered { get; set; }

    [DisplayName("自动发布")]
    public bool AutoPublish { get; set; }

    [DisplayName("更新时间")]
    [DateColumn(Format = "YYYY-MM-DD HH:mm", FromNow = true)]
    public DateTime? UpdatedAt { get; set; }
}