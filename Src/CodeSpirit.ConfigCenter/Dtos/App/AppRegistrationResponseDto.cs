namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用注册响应DTO
/// </summary>
public class AppRegistrationResponseDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// 应用密钥
    /// </summary>
    public string Secret { get; set; }
} 