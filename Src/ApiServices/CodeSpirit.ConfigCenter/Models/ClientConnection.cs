namespace CodeSpirit.ConfigCenter.Models;

/// <summary>
/// 客户端连接信息
/// </summary>
public class ClientConnection
{
    /// <summary>
    /// 客户端ID（连接ID）
    /// </summary>
    public string ConnectionId { get; set; }
    
    /// <summary>
    /// 客户端标识
    /// </summary>
    public string ClientId { get; set; }
    
    /// <summary>
    /// 应用ID
    /// </summary>
    public string AppId { get; set; }
    
    /// <summary>
    /// 环境
    /// </summary>
    public string Environment { get; set; }
    
    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string IpAddress { get; set; }
    
    /// <summary>
    /// 主机名
    /// </summary>
    public string HostName { get; set; }
    
    /// <summary>
    /// 客户端版本
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// 连接时间
    /// </summary>
    public DateTime ConnectedTime { get; set; }
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActiveTime { get; set; }
    
    /// <summary>
    /// 订阅的配置组
    /// </summary>
    public List<string> SubscribedGroups { get; set; } = new();
} 