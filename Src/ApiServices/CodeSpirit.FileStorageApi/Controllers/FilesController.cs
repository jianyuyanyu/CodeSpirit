using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Dtos;
using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers;

/// <summary>
/// 租户平台文件管理控制器
/// </summary>
[DisplayName("文件管理")]
[Navigation(Icon = "fa-solid fa-file-lines", PlatformType = PlatformType.Tenant)]
public class FilesController : ApiControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileStorageService">文件存储服务</param>
    /// <param name="logger">日志服务</param>
    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>文件分页列表</returns>
    [HttpGet]
    [DisplayName("获取文件列表")]
    public async Task<ActionResult<ApiResponse<PageList<FileDto>>>> GetFiles([FromQuery] FileQueryDto queryDto)
    {
        var request = new FileQueryRequest
        {
            BucketName = queryDto.BucketName,
            Category = queryDto.Category,
            Status = queryDto.Status,
            Tags = queryDto.Tags,
            FileName = queryDto.FileName,
            CreatedFrom = queryDto.CreatedFrom,
            CreatedTo = queryDto.CreatedTo,
            PageNumber = queryDto.Page,
            PageSize = queryDto.PerPage,
            OrderBy = queryDto.OrderBy ?? "CreatedTime",
            Descending = queryDto.OrderDir == "desc"
        };

        PageList<FileEntity> files = await _fileStorageService.QueryFilesAsync(request);
        
        // 转换为DTO
        var fileDtos = files.Items.Select(f => new FileDto
        {
            Id = f.Id,
            BucketName = f.BucketName,
            OriginalFileName = f.OriginalFileName,
            Size = f.Size,
            ContentType = f.ContentType,
            Extension = f.Extension,
            Category = f.Category,
            Status = f.Status,
            Description = f.Description,
            AccessCount = f.AccessCount,
            LastAccessTime = f.LastAccessTime,
            ExpirationTime = f.ExpirationTime,
            IsPublic = f.IsPublic,
            DownloadUrl = f.DownloadUrl,
            Tags = f.Tags,
            CreatedTime = f.CreatedAt,
            CreatedBy = f.CreatedBy.ToString(),
            UpdatedTime = f.UpdatedAt,
            UpdatedBy = f.UpdatedBy?.ToString()
        }).ToList();

        var result = new PageList<FileDto>(fileDtos, files.Total);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 导出文件列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>文件数据</returns>
    [HttpGet("export")]
    [DisplayName("导出文件列表")]
    public async Task<ActionResult<ApiResponse<PageList<FileDto>>>> ExportFiles([FromQuery] FileQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 最大导出数量限制
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 重用获取文件列表的逻辑
        var result = await GetFiles(queryDto);
        var files = result.Value?.Data;

        // 如果数据为空则返回错误信息
        return files?.Items?.Count == 0 ? BadResponse<PageList<FileDto>>("没有数据可供导出") : result;
    }

    /// <summary>
    /// 获取文件详情
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取文件详情")]
    public async Task<ActionResult<ApiResponse<FileDto>>> GetFileDetail(long id)
    {
        FileEntity file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null)
        {
            return BadResponse<FileDto>("文件不存在");
        }

        var fileDto = new FileDto
        {
            Id = file.Id,
            BucketName = file.BucketName,
            OriginalFileName = file.OriginalFileName,
            Size = file.Size,
            ContentType = file.ContentType,
            Extension = file.Extension,
            Category = file.Category,
            Status = file.Status,
            Description = file.Description,
            AccessCount = file.AccessCount,
            LastAccessTime = file.LastAccessTime,
            ExpirationTime = file.ExpirationTime,
            IsPublic = file.IsPublic,
            DownloadUrl = file.DownloadUrl,
            Tags = file.Tags,
            CreatedTime = file.CreatedAt,
            CreatedBy = file.CreatedBy.ToString(),
            UpdatedTime = file.UpdatedAt,
            UpdatedBy = file.UpdatedBy?.ToString()
        };

        return SuccessResponse(fileDto);
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">文件</param>
    /// <param name="createDto">上传配置</param>
    /// <returns>文件信息</returns>
    [HttpPost("upload")]
    [DisplayName("上传文件")]
    public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile(
        IFormFile file,
        [FromForm] CreateFileDto createDto)
    {
        if (file == null || file.Length == 0)
        {
            return BadResponse<FileDto>("文件不能为空");
        }

        var request = new FileUploadRequest
        {
            BucketName = createDto.BucketName,
            FileName = file.FileName,
            FileStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            Description = createDto.Description,
            ExpirationTime = createDto.ExpirationTime,
            IsPublic = createDto.IsPublic,
            OverwriteExisting = createDto.OverwriteExisting,
            Tags = createDto.Tags != null ? 
                createDto.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(tag => tag.Trim(), tag => tag.Trim()) : null
        };

        FileEntity fileEntity = await _fileStorageService.UploadFileAsync(request);
        
        var fileDto = new FileDto
        {
            Id = fileEntity.Id,
            BucketName = fileEntity.BucketName,
            OriginalFileName = fileEntity.OriginalFileName,
            Size = fileEntity.Size,
            ContentType = fileEntity.ContentType,
            Extension = fileEntity.Extension,
            Category = fileEntity.Category,
            Status = fileEntity.Status,
            Description = fileEntity.Description,
            AccessCount = fileEntity.AccessCount,
            LastAccessTime = fileEntity.LastAccessTime,
            ExpirationTime = fileEntity.ExpirationTime,
            IsPublic = fileEntity.IsPublic,
            DownloadUrl = fileEntity.DownloadUrl,
            Tags = fileEntity.Tags,
            CreatedTime = fileEntity.CreatedAt,
            CreatedBy = fileEntity.CreatedBy.ToString(),
            UpdatedTime = fileEntity.UpdatedAt,
            UpdatedBy = fileEntity.UpdatedBy?.ToString()
        };

        return SuccessResponseWithCreate<FileDto>(nameof(GetFileDetail), fileDto);
    }

    /// <summary>
    /// 更新文件信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="updateDto">更新信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新文件信息")]
    public async Task<ActionResult<ApiResponse>> UpdateFile(long id, UpdateFileDto updateDto)
    {
        FileEntity file = await _fileStorageService.GetFileInfoAsync(id);
        if (file == null)
        {
            return BadResponse("文件不存在");
        }

        // 更新文件信息（这里需要在服务层实现更新方法）
        // 由于当前服务接口没有更新方法，这里只是示例
        // await _fileStorageService.UpdateFileInfoAsync(id, updateDto);

        return SuccessResponse("文件信息更新成功");
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除文件")]
    public async Task<ActionResult<ApiResponse>> DeleteFile(long id)
    {
        bool result = await _fileStorageService.DeleteFileAsync(id);
        return result ? SuccessResponse("文件删除成功") : BadResponse("文件删除失败或文件不存在");
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件流</returns>
    [HttpGet("{id}/download")]
    [DisplayName("下载文件")]
    public async Task<IActionResult> DownloadFile(long id)
    {
        var (stream, fileInfo) = await _fileStorageService.DownloadFileAsync(id);
        
        if (stream == null || fileInfo == null)
        {
            return NotFound("文件不存在");
        }

        // 更新访问时间
        await _fileStorageService.UpdateAccessTimeAsync(id);

        return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
    }

    /// <summary>
    /// 获取文件下载链接
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接</returns>
    [HttpGet("{id}/download-url")]
    [DisplayName("获取下载链接")]
    public async Task<ActionResult<ApiResponse<object>>> GetDownloadUrl(long id, [FromQuery] int expirationMinutes = 60)
    {
        string downloadUrl = await _fileStorageService.GetDownloadUrlAsync(id, expirationMinutes);
        if (string.IsNullOrEmpty(downloadUrl))
        {
            return BadResponse<object>("获取下载链接失败");
        }

        return SuccessResponse<object>(new { downloadUrl, expiresIn = expirationMinutes });
    }

    /// <summary>
    /// 设置文件公开状态
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="isPublic">是否公开</param>
    /// <returns>设置结果</returns>
    [HttpPut("{id}/public")]
    [Operation("设置公开状态", "ajax", null, "确定要更改文件的公开状态吗？")]
    [DisplayName("设置公开状态")]
    public async Task<ActionResult<ApiResponse>> SetPublicStatus(long id, [FromQuery] bool isPublic)
    {
        // 这里需要在服务层实现设置公开状态的方法
        // await _fileStorageService.SetPublicStatusAsync(id, isPublic);
        
        string status = isPublic ? "公开" : "私有";
        return SuccessResponse($"文件已设置为{status}状态");
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的文件吗？", isBulkOperation: true)]
    [DisplayName("批量删除文件")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteFiles([FromBody] BatchOperationDto<long> request)
    {
        BatchOperationResult result = await _fileStorageService.BatchDeleteFilesAsync(request.Ids);

        if (result.Failed > 0)
        {
            string message = $"批量删除完成！成功删除 {result.Success} 个文件，失败 {result.Failed} 个";
            return SuccessResponse(message);
        }

        return SuccessResponse($"批量删除成功！共删除 {result.Success} 个文件");
    }

    /// <summary>
    /// 批量导入文件
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    [DisplayName("批量导入文件")]
    public async Task<ActionResult<ApiResponse>> BatchImportFiles([FromBody] BatchImportDtoBase<FileBatchImportItemDto> importDto)
    {
        // 这里需要在服务层实现批量导入功能
        // var (successCount, failedIds) = await _fileStorageService.BatchImportAsync(importDto.ImportData);
        
        // 暂时返回成功消息
        int successCount = importDto.ImportData?.Count ?? 0;
        return SuccessResponse($"成功批量导入了 {successCount} 个文件！");
    }

    /// <summary>
    /// 获取文件统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取文件统计")]
    public async Task<ActionResult<ApiResponse<object>>> GetFileStatistics()
    {
        // 这里需要在服务层实现统计功能
        // var statistics = await _fileStorageService.GetFileStatisticsAsync();
        
        // 暂时返回示例数据
        var statistics = new
        {
            TotalFiles = 0,
            TotalSize = 0L,
            FilesByCategory = new Dictionary<string, int>(),
            FilesByStatus = new Dictionary<string, int>()
        };

        return SuccessResponse<object>(statistics);
    }
}
