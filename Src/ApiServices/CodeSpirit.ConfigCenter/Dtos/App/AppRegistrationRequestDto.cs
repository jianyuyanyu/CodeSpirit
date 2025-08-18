namespace CodeSpirit.ConfigCenter.Dtos.App;

/// <summary>
/// 应用注册请求DTO
/// </summary>
public class AppRegistrationRequestDto
{
    /// <summary>
    /// 应用ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string Id { get; set; }
    
    /// <summary>
    /// 应用名称
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; }
    
    /// <summary>
    /// 应用描述
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; }
    
    /// <summary>
    /// 应用密钥
    /// </summary>
    [StringLength(100)]
    public string Secret { get; set; }
} 