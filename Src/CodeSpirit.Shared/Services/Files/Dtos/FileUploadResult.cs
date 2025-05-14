namespace CodeSpirit.Shared.Services.Files.Dtos;

/// <summary>
/// 文件上传结果
/// </summary>
public class FileUploadResult
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public string FileId { get; set; }
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; }
    
    /// <summary>
    /// 文件URL
    /// </summary>
    public string FileUrl { get; set; }
}
