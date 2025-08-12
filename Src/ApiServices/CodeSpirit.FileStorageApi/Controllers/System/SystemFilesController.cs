using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Services.System;
using CodeSpirit.Shared.Dtos.Common;
using static CodeSpirit.FileStorageApi.Services.System.ISystemFileService;
using static CodeSpirit.FileStorageApi.Services.System.ISystemTenantStorageService;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers.System;

/// <summary>
/// 系统平台文件管理控制器
/// </summary>
[DisplayName("文件管理")]
[Navigation(Icon = "fa-solid fa-file-lines", PlatformType = PlatformType.System)]
public class SystemFilesController : ApiControllerBase
{
    private readonly ISystemFileService _fileService;
    private readonly ILogger<SystemFilesController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileService">文件服务</param>
    /// <param name="logger">日志服务</param>
    public SystemFilesController(
        ISystemFileService fileService,
        ILogger<SystemFilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统文件列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统文件分页列表</returns>
    [HttpGet]
    [DisplayName("获取系统文件列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemFileDto>>>> GetSystemFiles([FromQuery] SystemFileQueryDto queryDto)
    {
        PageList<SystemFileDto> files = await _fileService.GetSystemFilesAsync(queryDto);
        return SuccessResponse(files);
    }

    /// <summary>
    /// 导出系统文件列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>文件数据</returns>
    [HttpGet("export")]
    [DisplayName("导出系统文件列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemFileDto>>>> ExportSystemFiles([FromQuery] SystemFileQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 50000; // 系统级导出数量上限更高
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取文件数据
        PageList<SystemFileDto> files = await _fileService.GetSystemFilesAsync(queryDto);

        // 如果数据为空则返回错误信息
        return files.Items.Count == 0 ? BadResponse<PageList<SystemFileDto>>("没有数据可供导出") : SuccessResponse(files);
    }

    /// <summary>
    /// 获取文件详情
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取文件详情")]
    public async Task<ActionResult<ApiResponse<SystemFileDto>>> GetFileDetail(long id)
    {
        SystemFileDto file = await _fileService.GetFileDetailAsync(id);
        return SuccessResponse(file);
    }

    /// <summary>
    /// 获取文件引用信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件引用信息</returns>
    [HttpGet("{id}/references")]
    [DisplayName("获取文件引用信息")]
    public async Task<ActionResult<ApiResponse<object>>> GetFileReferences(long id)
    {
        var references = await _fileService.GetFileReferencesAsync(id);
        return SuccessResponse(references);
    }

    /// <summary>
    /// 强制删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}/force")]
    [Operation("强制删除", "ajax", null, "确定要强制删除此文件吗？此操作不可撤销，将同时删除所有引用！")]
    [DisplayName("强制删除文件")]
    public async Task<ActionResult<ApiResponse>> ForceDeleteFile(long id)
    {
        await _fileService.ForceDeleteFileAsync(id);
        return SuccessResponse("文件已强制删除");
    }

    /// <summary>
    /// 恢复已删除的文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>恢复结果</returns>
    [HttpPost("{id}/restore")]
    [Operation("恢复文件", "ajax", null, "确定要恢复此文件吗？")]
    [DisplayName("恢复文件")]
    public async Task<ActionResult<ApiResponse>> RestoreFile(long id)
    {
        await _fileService.RestoreFileAsync(id);
        return SuccessResponse("文件已恢复");
    }

    /// <summary>
    /// 移动文件到其他存储桶
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>移动结果</returns>
    [HttpPost("{id}/move")]
    [Operation("移动文件", "form", null, "确定要将此文件移动到指定存储桶吗？")]
    [DisplayName("移动文件")]
    public async Task<ActionResult<ApiResponse>> MoveFile(long id, [FromBody] string targetBucketName)
    {
        await _fileService.MoveFileAsync(id, targetBucketName);
        return SuccessResponse($"文件已移动到存储桶 '{targetBucketName}'");
    }

    /// <summary>
    /// 获取文件下载链接
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接</returns>
    [HttpGet("{id}/download-url")]
    [DisplayName("获取文件下载链接")]
    public async Task<ActionResult<ApiResponse<object>>> GetFileDownloadUrl(long id, [FromQuery] int expirationMinutes = 60)
    {
        var result = await _fileService.GetFileDownloadUrlAsync(id, expirationMinutes);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 修复文件数据完整性
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>修复结果</returns>
    [HttpPost("{id}/repair")]
    [Operation("修复文件", "ajax", null, "确定要修复此文件的数据完整性吗？")]
    [DisplayName("修复文件数据")]
    public async Task<ActionResult<ApiResponse<FileRepairResult>>> RepairFile(long id)
    {
        var result = await _fileService.RepairFileAsync(id);
        return SuccessResponse(result, result.IsRepaired ? "文件修复成功" : "文件无需修复");
    }

    /// <summary>
    /// 获取系统文件统计摘要
    /// </summary>
    /// <returns>文件统计摘要</returns>
    [HttpGet("statistics/summary")]
    [DisplayName("获取文件统计摘要")]
    public async Task<ActionResult<ApiResponse<object>>> GetFileStatisticsSummary()
    {
        var summary = await _fileService.GetFileStatisticsSummaryAsync();
        return SuccessResponse(summary);
    }

    /// <summary>
    /// 获取文件类型分布统计
    /// </summary>
    /// <returns>文件类型分布</returns>
    [HttpGet("statistics/type-distribution")]
    [DisplayName("获取文件类型分布")]
    public async Task<ActionResult<ApiResponse<object>>> GetFileTypeDistribution()
    {
        var distribution = await _fileService.GetFileTypeDistributionAsync();
        return SuccessResponse(distribution);
    }

    /// <summary>
    /// 获取存储使用趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>存储使用趋势</returns>
    [HttpGet("statistics/storage-trend")]
    [DisplayName("获取存储使用趋势")]
    public async Task<ActionResult<ApiResponse<object>>> GetStorageUsageTrend([FromQuery] int days = 30)
    {
        var trend = await _fileService.GetStorageUsageTrendAsync(days);
        return SuccessResponse(trend);
    }

    /// <summary>
    /// 清理过期文件
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup/expired")]
    [Operation("清理过期文件", "ajax", null, "确定要清理所有过期文件吗？此操作不可撤销！")]
    [DisplayName("清理过期文件")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupExpiredFiles()
    {
        var result = await _fileService.CleanupExpiredFilesAsync();
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 个过期文件");
    }

    /// <summary>
    /// 清理无引用文件
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup/unreferenced")]
    [Operation("清理无引用文件", "ajax", null, "确定要清理所有无引用的文件吗？此操作不可撤销！")]
    [DisplayName("清理无引用文件")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupUnreferencedFiles()
    {
        var result = await _fileService.CleanupUnreferencedFilesAsync();
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 个无引用文件");
    }

    /// <summary>
    /// 批量删除文件
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的文件吗？", isBulkOperation: true)]
    [DisplayName("批量删除文件")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchDeleteFiles([FromBody] BatchOperationDto<long> request)
    {
        var result = await _fileService.BatchDeleteFilesAsync(request.Ids);
        return SuccessResponse(result, $"批量删除完成！成功删除 {result.Success} 个文件");
    }

    /// <summary>
    /// 批量恢复文件
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>恢复结果</returns>
    [HttpPost("batch/restore")]
    [Operation("批量恢复", "ajax", null, "确定要批量恢复选中的文件吗？", isBulkOperation: true)]
    [DisplayName("批量恢复文件")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchRestoreFiles([FromBody] BatchOperationDto<long> request)
    {
        var result = await _fileService.BatchRestoreFilesAsync(request.Ids);
        return SuccessResponse(result, $"批量恢复完成！成功恢复 {result.Success} 个文件");
    }

    /// <summary>
    /// 批量移动文件
    /// </summary>
    /// <param name="request">批量移动请求</param>
    /// <returns>移动结果</returns>
    [HttpPost("batch/move")]
    [Operation("批量移动", "form", null, "确定要将选中的文件移动到指定存储桶吗？", isBulkOperation: true)]
    [DisplayName("批量移动文件")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchMoveFiles([FromBody] BatchMoveFilesRequest request)
    {
        var result = await _fileService.BatchMoveFilesAsync(request.FileIds, request.TargetBucketName);
        return SuccessResponse(result, $"批量移动完成！成功移动 {result.Success} 个文件到存储桶 '{request.TargetBucketName}'");
    }
}

/// <summary>
/// 批量移动文件请求
/// </summary>
public class BatchMoveFilesRequest
{
    /// <summary>
    /// 文件ID列表
    /// </summary>
    [DisplayName("文件ID列表")]
    public List<long> FileIds { get; set; } = new();

    /// <summary>
    /// 目标存储桶名称
    /// </summary>
    [DisplayName("目标存储桶")]
    public string TargetBucketName { get; set; } = string.Empty;
}
