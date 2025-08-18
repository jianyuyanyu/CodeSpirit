using CodeSpirit.Shared.Entities;

namespace CodeSpirit.ConfigCenter.Models;

/// <summary>
/// 客户端应用实例
/// </summary>
public class ClientApp : EntityBase<int>
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    [DisplayName("应用ID")]
    public required string AppId { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    [Required]
    [StringLength(50)]
    [DisplayName("客户端IP")]
    public required string ClientIp { get; set; }

    /// <summary>
    /// 客户端标识
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("客户端标识")]
    public required string ClientId { get; set; }

    /// <summary>
    /// 机器名
    /// </summary>
    [Required]
    [StringLength(100)]
    [DisplayName("机器名")]
    public required string MachineName { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [StringLength(100)]
    [DisplayName("操作系统")]
    public string OsVersion { get; set; }

    /// <summary>
    /// 运行时版本
    /// </summary>
    [StringLength(50)]
    [DisplayName("运行时版本")]
    public string RuntimeVersion { get; set; }

    /// <summary>
    /// 最后心跳时间
    /// </summary>
    [Required]
    [DisplayName("最后心跳时间")]
    public required DateTime LastHeartbeat { get; set; }

    /// <summary>
    /// 当前配置版本
    /// </summary>
    [Required]
    [DisplayName("配置版本")]
    public required long Version { get; set; }

    /// <summary>
    /// 所属应用
    /// </summary>
    public App App { get; set; }
} 