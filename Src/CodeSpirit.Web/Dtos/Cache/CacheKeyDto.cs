using System.ComponentModel;

namespace CodeSpirit.Web.Dtos.Cache;

/// <summary>
/// 缓存键信息 DTO
/// </summary>
public class CacheKeyDto
{
    /// <summary>
    /// 缓存键
    /// </summary>
    [DisplayName("缓存键")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型
    /// </summary>
    [DisplayName("数据类型")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（秒），-1表示永不过期，-2表示键不存在
    /// </summary>
    [DisplayName("过期时间（秒）")]
    public long? Ttl { get; set; }

    /// <summary>
    /// 内存大小（字节）
    /// </summary>
    [DisplayName("内存大小（字节）")]
    public long? Size { get; set; }
}

