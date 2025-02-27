namespace CodeSpirit.ConfigCenter.Client.Models;

/// <summary>
/// 应用注册请求
/// </summary>
public class AppRegistrationRequest
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// 应用名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 应用描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 应用密钥
    /// </summary>
    public string Secret { get; set; }
} 