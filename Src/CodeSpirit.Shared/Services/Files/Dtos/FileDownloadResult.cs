namespace CodeSpirit.Shared.Services.Files.Dtos;

/// <summary>
/// 文件下载结果
/// </summary>
public class FileDownloadResult
{
    /// <summary>
    /// 获取或设置文件流
    /// </summary>
    public Stream FileStream { get; set; }

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
    /// 获取或设置是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置错误信息
    /// </summary>
    public string ErrorMessage { get; set; }
}
