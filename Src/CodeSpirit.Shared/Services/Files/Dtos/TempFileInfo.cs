namespace CodeSpirit.Shared.Services.Files.Dtos;

/// <summary>
/// 文件信息
/// </summary>
public class TempFileInfo
{
    /// <summary>
    /// 获取或设置文件ID
    /// </summary>
    public string FileId { get; set; }

    /// <summary>
    /// 获取或设置文件名
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 获取或设置内容类型
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// 获取或设置文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 获取或设置创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 获取或设置是否存储在缓存中
    /// </summary>
    public bool IsInCache { get; set; }

    /// <summary>
    /// 获取或设置缓存过期时间
    /// </summary>
    public DateTime? CacheExpireTime { get; set; }
}
