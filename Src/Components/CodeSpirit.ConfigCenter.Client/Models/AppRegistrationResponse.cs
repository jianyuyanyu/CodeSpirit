namespace CodeSpirit.ConfigCenter.Client.Models;

/// <summary>
/// 应用注册响应
/// </summary>
public class AppRegistrationResponse
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// 应用密钥
    /// </summary>
    public string Secret { get; set; }
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; }
} 