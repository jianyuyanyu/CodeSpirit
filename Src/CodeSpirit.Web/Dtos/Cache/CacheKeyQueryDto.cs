using CodeSpirit.Core.Dtos;
using System.ComponentModel;

namespace CodeSpirit.Web.Dtos.Cache;

/// <summary>
/// 缓存键查询 DTO
/// </summary>
public class CacheKeyQueryDto : QueryDtoBase
{
    /// <summary>
    /// 键名模式（支持通配符，如 CodeSpirit:*:user:*）
    /// </summary>
    [DisplayName("键名模式")]
    public string? Pattern { get; set; }

    /// <summary>
    /// 租户ID（用于过滤租户相关的缓存键）
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }
}

