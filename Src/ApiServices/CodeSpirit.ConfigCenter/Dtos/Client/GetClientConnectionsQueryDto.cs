using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Dtos.Client;

/// <summary>
/// 获取客户端连接查询参数
/// </summary>
public class GetClientConnectionsQueryDto
{
    /// <summary>
    /// 按应用ID筛选
    /// </summary>
    [DisplayName("应用ID")]
    public string AppId { get; set; }
    
    /// <summary>
    /// 按环境筛选
    /// </summary>
    [DisplayName("环境")]
    public string Environment { get; set; }
} 