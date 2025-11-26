using System.ComponentModel;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.IdentityApi.Dtos.ApiKey;

/// <summary>
/// API密钥查询条件DTO
/// </summary>
public class ApiKeyQueryDto : QueryDtoBase
{
    /// <summary>
    /// 密钥名称（模糊查询）
    /// </summary>
    [DisplayName("密钥名称")]
    public string? Name { get; set; }

    /// <summary>
    /// 是否启用（可选）
    /// </summary>
    [DisplayName("是否启用")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// 是否已过期（可选）
    /// </summary>
    [DisplayName("是否已过期")]
    public bool? IsExpired { get; set; }

    /// <summary>
    /// 用户ID（可选）
    /// </summary>
    [DisplayName("用户ID")]
    public long? UserId { get; set; }
}

