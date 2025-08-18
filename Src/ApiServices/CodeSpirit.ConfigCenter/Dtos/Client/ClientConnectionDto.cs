using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Dtos.Client;

/// <summary>
/// 客户端连接数据传输对象
/// </summary>
public class ClientConnectionDto
{
    /// <summary>
    /// 连接ID
    /// </summary>
    [DisplayName("连接ID")]
    public string ConnectionId { get; set; }
    
    /// <summary>
    /// 客户端ID
    /// </summary>
    [DisplayName("客户端ID")]
    public string ClientId { get; set; }
    
    /// <summary>
    /// 应用ID
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }
    
    /// <summary>
    /// 环境
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }
    
    /// <summary>
    /// 客户端IP地址
    /// </summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 主机名
    /// </summary>
    [DisplayName("主机名")]
    public string HostName { get; set; }
    
    /// <summary>
    /// 客户端版本
    /// </summary>
    [DisplayName("版本")]
    public string Version { get; set; }
    
    /// <summary>
    /// 连接时间
    /// </summary>
    [DisplayName("连接时间")]
    public DateTime ConnectedTime { get; set; }
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    [DisplayName("最后活动时间")]
    public DateTime LastActiveTime { get; set; }
    
    /// <summary>
    /// 在线时长（分钟）
    /// </summary>
    [DisplayName("在线时长(分钟)")]
    public double OnlineDurationMinutes => Math.Round((DateTime.UtcNow - ConnectedTime).TotalMinutes, 2);

    [DisplayName("状态")]
    public string Status { get; set; }
} 