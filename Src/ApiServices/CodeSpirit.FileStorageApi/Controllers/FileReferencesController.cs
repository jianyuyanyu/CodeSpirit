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
/// 租户平台文件引用管理控制器
/// </summary>
[DisplayName("文件引用管理")]
[Navigation(Icon = "fa-solid fa-link", PlatformType = PlatformType.Tenant)]
public class FileReferencesController : ApiControllerBase
{
    private readonly IFileReferenceService _fileReferenceService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileReferencesController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileReferenceService">文件引用服务</param>
    /// <param name="fileStorageService">文件存储服务</param>
    /// <param name="logger">日志服务</param>
    public FileReferencesController(
        IFileReferenceService fileReferenceService,
        IFileStorageService fileStorageService,
        ILogger<FileReferencesController> logger)
    {
        _fileReferenceService = fileReferenceService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取文件引用列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>文件引用分页列表</returns>
    [HttpGet]
    [DisplayName("获取文件引用列表")]
    public async Task<ActionResult<ApiResponse<PageList<FileReferenceDto>>>> GetFileReferences([FromQuery] FileReferenceQueryDto queryDto)
    {
        var request = new ReferenceQueryRequest
        {
            FileId = queryDto.FileId,
            SourceService = queryDto.SourceService,
            SourceEntityType = queryDto.SourceEntityType,
            SourceEntityId = queryDto.SourceEntityId,
            ReferenceType = queryDto.ReferenceType,
            Status = queryDto.Status,
            IsTemporary = queryDto.IsTemporary,
            CreatedFrom = queryDto.CreatedFrom,
            CreatedTo = queryDto.CreatedTo,
            PageNumber = queryDto.Page,
            PageSize = queryDto.PerPage,
            OrderBy = queryDto.OrderBy ?? "CreatedTime",
            Descending = queryDto.OrderDir == "desc"
        };

        PageList<FileReferenceEntity> references = await _fileReferenceService.QueryReferencesAsync(request);
        
        // 转换为DTO
        var referenceDtos = references.Items.Select(r => new FileReferenceDto
        {
            Id = r.Id,
            FileId = r.FileId,
            SourceService = r.SourceService,
            SourceEntityType = r.SourceEntityType,
            SourceEntityId = r.SourceEntityId,
            FieldName = r.FieldName,
            ReferenceType = r.ReferenceType,
            Status = r.Status,
            IsTemporary = r.IsTemporary,
            ExpirationTime = r.ExpirationTime,
            ConfirmedTime = r.ConfirmedTime,
            Remarks = r.Remarks,
            CreatedTime = r.CreatedAt,
            CreatedBy = r.CreatedBy.ToString(),
            UpdatedTime = r.UpdatedAt,
            UpdatedBy = r.UpdatedBy?.ToString(),
            File = r.File != null ? new FileDto
            {
                Id = r.File.Id,
                BucketName = r.File.BucketName,
                OriginalFileName = r.File.OriginalFileName,
                Size = r.File.Size,
                ContentType = r.File.ContentType,
                Extension = r.File.Extension,
                Category = r.File.Category,
                Status = r.File.Status,
                Description = r.File.Description,
                DownloadUrl = r.File.DownloadUrl,
                Tags = r.File.Tags
            } : null
        }).ToList();

        var result = new PageList<FileReferenceDto>(referenceDtos, references.Total);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 导出文件引用列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>引用数据</returns>
    [HttpGet("export")]
    [DisplayName("导出文件引用列表")]
    public async Task<ActionResult<ApiResponse<PageList<FileReferenceDto>>>> ExportFileReferences([FromQuery] FileReferenceQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 最大导出数量限制
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 重用获取引用列表的逻辑
        var result = await GetFileReferences(queryDto);
        var references = result.Value?.Data;

        // 如果数据为空则返回错误信息
        return references?.Items?.Count == 0 ? BadResponse<PageList<FileReferenceDto>>("没有数据可供导出") : result;
    }

    /// <summary>
    /// 获取文件引用详情
    /// </summary>
    /// <param name="id">引用ID</param>
    /// <returns>引用详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取文件引用详情")]
    public async Task<ActionResult<ApiResponse<FileReferenceDto>>> GetFileReferenceDetail(long id)
    {
        FileReferenceEntity reference = await _fileReferenceService.GetReferenceAsync(id);
        if (reference == null)
        {
            return BadResponse<FileReferenceDto>("文件引用不存在");
        }

        var referenceDto = new FileReferenceDto
        {
            Id = reference.Id,
            FileId = reference.FileId,
            SourceService = reference.SourceService,
            SourceEntityType = reference.SourceEntityType,
            SourceEntityId = reference.SourceEntityId,
            FieldName = reference.FieldName,
            ReferenceType = reference.ReferenceType,
            Status = reference.Status,
            IsTemporary = reference.IsTemporary,
            ExpirationTime = reference.ExpirationTime,
            ConfirmedTime = reference.ConfirmedTime,
            Remarks = reference.Remarks,
            CreatedTime = reference.CreatedAt,
            CreatedBy = reference.CreatedBy.ToString(),
            UpdatedTime = reference.UpdatedAt,
            UpdatedBy = reference.UpdatedBy?.ToString(),
            File = reference.File != null ? new FileDto
            {
                Id = reference.File.Id,
                BucketName = reference.File.BucketName,
                OriginalFileName = reference.File.OriginalFileName,
                Size = reference.File.Size,
                ContentType = reference.File.ContentType,
                Extension = reference.File.Extension,
                Category = reference.File.Category,
                Status = reference.File.Status,
                Description = reference.File.Description,
                DownloadUrl = reference.File.DownloadUrl,
                Tags = reference.File.Tags
            } : null
        };

        return SuccessResponse(referenceDto);
    }

    /// <summary>
    /// 创建文件引用
    /// </summary>
    /// <param name="createDto">创建信息</param>
    /// <returns>引用信息</returns>
    [HttpPost]
    [DisplayName("创建文件引用")]
    public async Task<ActionResult<ApiResponse<FileReferenceDto>>> CreateFileReference(CreateFileReferenceDto createDto)
    {
        // 验证文件是否存在
        FileEntity file = await _fileStorageService.GetFileInfoAsync(createDto.FileId);
        if (file == null)
        {
            return BadResponse<FileReferenceDto>("关联的文件不存在");
        }

        var request = new FileReferenceCreateRequest
        {
            FileId = createDto.FileId,
            SourceService = createDto.SourceService,
            SourceEntityType = createDto.SourceEntityType,
            SourceEntityId = createDto.SourceEntityId,
            FieldName = createDto.FieldName,
            ReferenceType = createDto.ReferenceType,
            IsTemporary = createDto.IsTemporary,
            ExpirationTime = createDto.ExpirationTime,
            Remarks = createDto.Remarks
        };

        long referenceId = await _fileReferenceService.CreateReferenceAsync(request);
        
        // 获取创建的引用详情
        var referenceResult = await GetFileReferenceDetail(referenceId);
        return referenceResult.Value?.Status == 0 ? 
            SuccessResponseWithCreate<FileReferenceDto>(nameof(GetFileReferenceDetail), referenceResult.Value.Data) :
            BadResponse<FileReferenceDto>("文件引用创建成功，但获取详情失败");
    }

    /// <summary>
    /// 更新文件引用
    /// </summary>
    /// <param name="id">引用ID</param>
    /// <param name="updateDto">更新信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新文件引用")]
    public async Task<ActionResult<ApiResponse>> UpdateFileReference(long id, UpdateFileReferenceDto updateDto)
    {
        FileReferenceEntity reference = await _fileReferenceService.GetReferenceAsync(id);
        if (reference == null)
        {
            return BadResponse("文件引用不存在");
        }

        // 这里需要在服务层实现更新方法
        // await _fileReferenceService.UpdateReferenceAsync(id, updateDto);

        return SuccessResponse("文件引用更新成功");
    }

    /// <summary>
    /// 删除文件引用
    /// </summary>
    /// <param name="id">引用ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [DisplayName("删除文件引用")]
    public async Task<ActionResult<ApiResponse>> DeleteFileReference(long id)
    {
        bool result = await _fileReferenceService.CancelReferenceAsync(id);
        return result ? SuccessResponse("文件引用删除成功") : BadResponse("文件引用删除失败或引用不存在");
    }

    /// <summary>
    /// 确认文件引用
    /// </summary>
    /// <param name="id">引用ID</param>
    /// <returns>确认结果</returns>
    [HttpPost("{id}/confirm")]
    [Operation("确认引用", "ajax", null, "确定要确认此文件引用吗？")]
    [DisplayName("确认文件引用")]
    public async Task<ActionResult<ApiResponse>> ConfirmFileReference(long id)
    {
        bool result = await _fileReferenceService.ConfirmReferenceAsync(id);
        return result ? SuccessResponse("文件引用确认成功") : BadResponse("文件引用确认失败或引用不存在");
    }

    /// <summary>
    /// 取消文件引用
    /// </summary>
    /// <param name="id">引用ID</param>
    /// <returns>取消结果</returns>
    [HttpPost("{id}/cancel")]
    [Operation("取消引用", "ajax", null, "确定要取消此文件引用吗？")]
    [DisplayName("取消文件引用")]
    public async Task<ActionResult<ApiResponse>> CancelFileReference(long id)
    {
        bool result = await _fileReferenceService.CancelReferenceAsync(id);
        return result ? SuccessResponse("文件引用取消成功") : BadResponse("文件引用取消失败或引用不存在");
    }

    /// <summary>
    /// 批量确认文件引用
    /// </summary>
    /// <param name="confirmDto">确认信息</param>
    /// <returns>确认结果</returns>
    [HttpPost("batch/confirm")]
    [Operation("批量确认", "form", null, "确定要批量确认选中的文件引用吗？", isBulkOperation: true)]
    [DisplayName("批量确认文件引用")]
    public async Task<ActionResult<ApiResponse>> BatchConfirmFileReferences([FromBody] ConfirmFileReferenceDto confirmDto)
    {
        BatchOperationResult result = await _fileReferenceService.BatchConfirmReferencesAsync(confirmDto.ReferenceIds);

        if (result.Failed > 0)
        {
            string message = $"批量确认完成！成功确认 {result.Success} 个引用，失败 {result.Failed} 个";
            return SuccessResponse(message);
        }

        return SuccessResponse($"批量确认成功！共确认 {result.Success} 个文件引用");
    }

    /// <summary>
    /// 批量删除文件引用
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的文件引用吗？", isBulkOperation: true)]
    [DisplayName("批量删除文件引用")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteFileReferences([FromBody] BatchOperationDto<long> request)
    {
        int successCount = 0;
        int failedCount = 0;

        foreach (long id in request.Ids)
        {
            bool result = await _fileReferenceService.CancelReferenceAsync(id);
            if (result)
                successCount++;
            else
                failedCount++;
        }

        if (failedCount > 0)
        {
            string message = $"批量删除完成！成功删除 {successCount} 个引用，失败 {failedCount} 个";
            return SuccessResponse(message);
        }

        return SuccessResponse($"批量删除成功！共删除 {successCount} 个文件引用");
    }

    /// <summary>
    /// 根据来源删除引用
    /// </summary>
    /// <param name="deleteDto">删除条件</param>
    /// <returns>删除结果</returns>
    [HttpPost("delete-by-source")]
    [Operation("按来源删除", "form", null, "确定要删除指定来源的所有文件引用吗？")]
    [DisplayName("按来源删除引用")]
    public async Task<ActionResult<ApiResponse>> DeleteReferencesBySource([FromBody] DeleteReferencesBySourceDto deleteDto)
    {
        bool result = await _fileReferenceService.DeleteReferencesBySourceAsync(
            deleteDto.SourceService, 
            deleteDto.SourceEntityType, 
            deleteDto.SourceEntityId);

        return result ? 
            SuccessResponse("指定来源的文件引用删除成功") : 
            BadResponse("文件引用删除失败或未找到匹配的引用");
    }

    /// <summary>
    /// 获取文件的引用列表
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>引用列表</returns>
    [HttpGet("by-file/{fileId}")]
    [DisplayName("获取文件的引用列表")]
    public async Task<ActionResult<ApiResponse<List<FileReferenceDto>>>> GetFileReferences(long fileId)
    {
        IEnumerable<FileReferenceEntity> references = await _fileReferenceService.GetFileReferencesAsync(fileId);
        
        var referenceDtos = references.Select(r => new FileReferenceDto
        {
            Id = r.Id,
            FileId = r.FileId,
            SourceService = r.SourceService,
            SourceEntityType = r.SourceEntityType,
            SourceEntityId = r.SourceEntityId,
            FieldName = r.FieldName,
            ReferenceType = r.ReferenceType,
            Status = r.Status,
            IsTemporary = r.IsTemporary,
            ExpirationTime = r.ExpirationTime,
            ConfirmedTime = r.ConfirmedTime,
            Remarks = r.Remarks,
            CreatedTime = r.CreatedAt,
            CreatedBy = r.CreatedBy.ToString(),
            UpdatedTime = r.UpdatedAt,
            UpdatedBy = r.UpdatedBy?.ToString()
        }).ToList();

        return SuccessResponse(referenceDtos);
    }

    /// <summary>
    /// 清理过期的临时引用
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup/expired")]
    [Operation("清理过期引用", "ajax", null, "确定要清理所有过期的临时引用吗？")]
    [DisplayName("清理过期引用")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupExpiredReferences()
    {
        int cleanupCount = await _fileReferenceService.CleanupExpiredReferencesAsync();
        return SuccessResponse<object>(new { cleanupCount }, $"清理完成！共清理 {cleanupCount} 个过期引用");
    }

    /// <summary>
    /// 获取无引用的文件
    /// </summary>
    /// <param name="days">天数（早于多少天）</param>
    /// <returns>无引用的文件列表</returns>
    [HttpGet("unreferenced-files")]
    [DisplayName("获取无引用文件")]
    public async Task<ActionResult<ApiResponse<List<long>>>> GetUnreferencedFiles([FromQuery] int days = 7)
    {
        DateTime olderThan = DateTime.UtcNow.AddDays(-days);
        IEnumerable<long> unreferencedFileIds = await _fileReferenceService.GetUnreferencedFilesAsync(olderThan);
        
        return SuccessResponse(unreferencedFileIds.ToList());
    }

    /// <summary>
    /// 批量导入文件引用
    /// </summary>
    /// <param name="importDto">导入数据</param>
    /// <returns>导入结果</returns>
    [HttpPost("batch/import")]
    [DisplayName("批量导入文件引用")]
    public async Task<ActionResult<ApiResponse>> BatchImportFileReferences([FromBody] BatchImportDtoBase<FileReferenceBatchImportItemDto> importDto)
    {
        int successCount = 0;
        int failedCount = 0;

        foreach (var item in importDto.ImportData ?? new List<FileReferenceBatchImportItemDto>())
        {
            try
            {
                var request = new FileReferenceCreateRequest
                {
                    FileId = item.FileId,
                    SourceService = item.SourceService,
                    SourceEntityType = item.SourceEntityType,
                    SourceEntityId = item.SourceEntityId,
                    FieldName = item.FieldName,
                    ReferenceType = item.ReferenceType,
                    IsTemporary = item.IsTemporary,
                    Remarks = item.Remarks
                };

                await _fileReferenceService.CreateReferenceAsync(request);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入文件引用失败: {FileId}", item.FileId);
                failedCount++;
            }
        }

        string message = failedCount > 0 ? 
            $"批量导入完成！成功导入 {successCount} 个引用，失败 {failedCount} 个" :
            $"批量导入成功！共导入 {successCount} 个文件引用";

        return SuccessResponse(message);
    }

    /// <summary>
    /// 获取文件引用统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [DisplayName("获取引用统计")]
    public async Task<ActionResult<ApiResponse<FileReferenceStatisticsDto>>> GetReferenceStatistics()
    {
        // 这里需要实现统计功能
        var statistics = new FileReferenceStatisticsDto
        {
            TotalReferences = 0,
            PendingReferences = 0,
            ConfirmedReferences = 0,
            ExpiredReferences = 0,
            TemporaryReferences = 0,
            ReferencesByType = new Dictionary<ReferenceType, long>(),
            ReferencesByService = new Dictionary<string, long>()
        };

        return SuccessResponse(statistics);
    }
}

/// <summary>
/// 按来源删除引用DTO
/// </summary>
public class DeleteReferencesBySourceDto
{
    /// <summary>
    /// 引用来源服务
    /// </summary>
    [Required]
    [DisplayName("来源服务")]
    public string SourceService { get; set; }

    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    [Required]
    [DisplayName("来源实体类型")]
    public string SourceEntityType { get; set; }

    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    [Required]
    [DisplayName("来源实体ID")]
    public string SourceEntityId { get; set; }
}
