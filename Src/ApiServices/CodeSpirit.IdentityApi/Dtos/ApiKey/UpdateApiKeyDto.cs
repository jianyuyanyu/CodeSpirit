using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// 更新API密钥DTO
/// </summary>
public class UpdateApiKeyDto
{
    /// <summary>
    /// API密钥名称
    /// </summary>
    [Required(ErrorMessage = "密钥名称不能为空")]
    [StringLength(100, ErrorMessage = "密钥名称长度不能超过100个字符")]
    [DisplayName("密钥名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// API密钥描述
    /// </summary>
    [StringLength(500, ErrorMessage = "密钥描述长度不能超过500个字符")]
    [DisplayName("密钥描述")]
    public string? Description { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [DisplayName("是否启用")]
    public bool IsActive { get; set; }
}

