using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Web.Dtos.Cache;

/// <summary>
/// 批量删除缓存 DTO
/// </summary>
public class BatchDeleteCacheDto
{
    /// <summary>
    /// 键名模式（支持通配符，如 CodeSpirit:*:user:*）
    /// </summary>
    [Required(ErrorMessage = "键名模式不能为空")]
    [DisplayName("键名模式")]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID（用于过滤租户相关的缓存键）
    /// </summary>
    [DisplayName("租户ID")]
    public string? TenantId { get; set; }
}

