using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.FileStorageApi.Controllers;

/// <summary>
/// 文件管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <param name="bucketName">存储桶名称</param>
    /// <param name="description">文件描述</param>
    /// <returns>文件信息</returns>
    [HttpPost("upload")]
    public async Task<ApiResponse<FileEntity>> UploadFileAsync(
        IFormFile file,
        [FromForm] string bucketName = "default",
        [FromForm] string? description = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<FileEntity>.Error(400, "文件不能为空");
            }

            var request = new FileUploadRequest
            {
                BucketName = bucketName,
                FileName = file.FileName,
                FileStream = file.OpenReadStream(),
                ContentType = file.ContentType,
                Description = description
            };

            var fileEntity = await _fileStorageService.UploadFileAsync(request);
            return ApiResponse<FileEntity>.Success(fileEntity, "文件上传成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败: {FileName}", file?.FileName);
            return ApiResponse<FileEntity>.Error(500, "文件上传失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件流</returns>
    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> DownloadFileAsync(long fileId)
    {
        try
        {
            var (stream, fileInfo) = await _fileStorageService.DownloadFileAsync(fileId);
            
            if (stream == null || fileInfo == null)
            {
                return NotFound("文件不存在");
            }

            return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件下载失败: {FileId}", fileId);
            return StatusCode(500, "文件下载失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>文件信息</returns>
    [HttpGet("{fileId}")]
    public async Task<ApiResponse<FileEntity>> GetFileInfoAsync(long fileId)
    {
        try
        {
            var fileInfo = await _fileStorageService.GetFileInfoAsync(fileId);
            
            if (fileInfo == null)
            {
                return ApiResponse<FileEntity>.Error(404, "文件不存在");
            }

            return ApiResponse<FileEntity>.Success(fileInfo, "获取文件信息成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息失败: {FileId}", fileId);
            return ApiResponse<FileEntity>.Error(500, "获取文件信息失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{fileId}")]
    public async Task<ApiResponse> DeleteFileAsync(long fileId)
    {
        try
        {
            var result = await _fileStorageService.DeleteFileAsync(fileId);
            
            if (!result)
            {
                return ApiResponse.Error(404, "文件不存在或删除失败");
            }

            return ApiResponse.Success("文件删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件删除失败: {FileId}", fileId);
            return ApiResponse.Error(500, "文件删除失败: " + ex.Message);
        }
    }
}
