using CodeSpirit.Shared.Services.Background.Dtos;
using CodeSpirit.Shared.Services.Files.Dtos;

namespace CodeSpirit.Shared.Services.Files;

/// <summary>
/// 文件服务接口
/// </summary>
public interface ITempFileService
{    
    /// <summary>
    /// 将文件上传到分布式缓存
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>上传结果</returns>
    /// <remarks>文件大小限制为512MB</remarks>
    /// <exception cref="InvalidOperationException">当文件大小超过512MB限制时抛出</exception>
    Task<FileUploadResult> UploadToCacheAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// 根据文件ID下载文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件下载结果</returns>
    Task<FileDownloadResult> DownloadFileAsync(string fileId);

    /// <summary>
    /// 根据文件ID获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    Task<TempFileInfo> GetFileInfoAsync(string fileId);
    
    /// <summary>
    /// 获取导出任务信息
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>导出任务信息</returns>
    Task<ExportTaskDto> GetExportTaskAsync(string taskId);
    
    /// <summary>
    /// 更新导出任务信息
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateExportTaskAsync(ExportTaskDto taskInfo);
}
