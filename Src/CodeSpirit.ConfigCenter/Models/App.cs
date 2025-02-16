using CodeSpirit.ConfigCenter.Models.Enums;
using CodeSpirit.Shared.Entities;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Models;
/// <summary>
/// 应用实体，代表一个使用配置中心的应用
/// </summary>
public class App : AuditableEntityBase<string>
{
    /// <summary>
    /// 应用名称
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("应用名称")]
    public required string Name { get; set; }

    /// <summary>
    /// 应用密钥，用于接口认证
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用密钥")]
    public required string Secret { get; set; }

    /// <summary>
    /// 应用描述
    /// </summary>
    [StringLength(200)]
    [DisplayName("应用描述")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("启用状态")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否自动发布配置变更
    /// </summary>
    [DisplayName("自动发布")]
    public bool AutoPublish { get; set; }

    /// <summary>
    /// 应用标签，用于分组和筛选
    /// </summary>
    [StringLength(50)]
    [DisplayName("应用标签")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// 是否为自动注册的应用
    /// </summary>
    [DisplayName("自动注册")]
    public bool IsAutoRegistered { get; set; }

    /// <summary>
    /// 继承的应用ID，用于配置继承
    /// </summary>
    [StringLength(36)]
    [DisplayName("继承应用ID")]
    public string InheritancedAppId { get; set; } = string.Empty;

    /// <summary>
    /// 继承的应用导航属性
    /// </summary>
    public App InheritancedApp { get; set; }

    /// <summary>
    /// 应用的配置项集合
    /// </summary>
    public ICollection<ConfigItem> ConfigItems { get; set; }
} 