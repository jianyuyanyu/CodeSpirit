using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Services.System;
using CodeSpirit.Shared.Dtos.Common;
using static CodeSpirit.FileStorageApi.Services.System.ISystemFileService;
using CodeSpirit.Amis.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers.System;

/// <summary>
/// 系统平台图片管理控制器
/// </summary>
[DisplayName("图片管理")]
[Navigation(Icon = "fa-solid fa-image", PlatformType = PlatformType.System)]
[AmisCard(
    DefaultPerPage = 12,
    SwitchPerPage = true,
    Placeholder = "暂无图片",
    ColumnsCount = 3,
    TitleField = "OriginalFileName",
    SubTitleField = "SizeFormatted",
    DescriptionField = "Description",
    AvatarField = "DownloadUrl"
)]
public class SystemImagesController : ApiControllerBase
{
    private readonly ISystemImageService _imageService;
    private readonly ILogger<SystemImagesController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="imageService">系统图片服务</param>
    /// <param name="logger">日志服务</param>
    public SystemImagesController(
        ISystemImageService imageService,
        ILogger<SystemImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统图片列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统图片分页列表</returns>
    [HttpGet]
    [DisplayName("获取系统图片列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemImageDto>>>> GetSystemImages([FromQuery] SystemImageQueryDto queryDto)
    {
        PageList<SystemImageDto> images = await _imageService.GetSystemImagesAsync(queryDto);
        return SuccessResponse(images);
    }

    /// <summary>
    /// 导出系统图片列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片数据</returns>
    [HttpGet("export")]
    [DisplayName("导出系统图片列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemImageDto>>>> ExportSystemImages([FromQuery] SystemImageQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000; // 系统级图片导出数量上限
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取图片数据
        PageList<SystemImageDto> images = await _imageService.GetSystemImagesAsync(queryDto);

        // 如果数据为空则返回错误信息
        return images.Items.Count == 0 ? BadResponse<PageList<SystemImageDto>>("没有数据可供导出") : SuccessResponse(images);
    }

    /// <summary>
    /// 获取图片详情
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取图片详情")]
    public async Task<ActionResult<ApiResponse<SystemImageDto>>> GetImageDetail(long id)
    {
        SystemImageDto image = await _imageService.GetImageDetailAsync(id);
        return SuccessResponse(image);
    }

    /// <summary>
    /// 获取图片引用信息
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片引用信息</returns>
    [HttpGet("{id}/references")]
    [DisplayName("获取图片引用信息")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageReferences(long id)
    {
        var references = await _imageService.GetImageReferencesAsync(id);
        return SuccessResponse(references);
    }

    /// <summary>
    /// 恢复已删除的图片
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>恢复结果</returns>
    [HttpPost("{id}/restore")]
    [Operation("恢复图片", "ajax", null, "确定要恢复此图片吗？")]
    [DisplayName("恢复图片")]
    public async Task<ActionResult<ApiResponse>> RestoreImage(long id)
    {
        await _imageService.RestoreImageAsync(id);
        return SuccessResponse("图片已恢复");
    }

    /// <summary>
    /// 移动图片到其他存储桶
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>移动结果</returns>
    [HttpPost("{id}/move")]
    [Operation("移动图片", "form", null, "确定要将此图片移动到指定存储桶吗？")]
    [DisplayName("移动图片")]
    public async Task<ActionResult<ApiResponse>> MoveImage(long id, [FromBody] string targetBucketName)
    {
        await _imageService.MoveImageAsync(id, targetBucketName);
        return SuccessResponse($"图片已移动到存储桶 '{targetBucketName}'");
    }

    /// <summary>
    /// 获取图片下载链接
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接</returns>
    [HttpGet("{id}/download-url")]
    [DisplayName("获取图片下载链接")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageDownloadUrl(long id, [FromQuery] int expirationMinutes = 60)
    {
        var result = await _imageService.GetImageDownloadUrlAsync(id, expirationMinutes);
        return SuccessResponse(result);
    }

    ///// <summary>
    ///// 修复图片数据完整性
    ///// </summary>
    ///// <param name="id">图片ID</param>
    ///// <returns>修复结果</returns>
    //[HttpPost("{id}/repair")]
    //[Operation("修复图片", "ajax", null, "确定要修复此图片的数据完整性吗？")]
    //[DisplayName("修复图片数据")]
    //public async Task<ActionResult<ApiResponse<FileRepairResult>>> RepairImage(long id)
    //{
    //    var result = await _imageService.RepairImageAsync(id);
    //    return SuccessResponse(result, result.IsRepaired ? "图片修复成功" : "图片无需修复");
    //}

    ///// <summary>
    ///// 重建图片元数据
    ///// </summary>
    ///// <param name="id">图片ID</param>
    ///// <returns>重建结果</returns>
    //[HttpPost("{id}/rebuild-metadata")]
    //[Operation("重建元数据", "ajax", null, "确定要重建此图片的元数据吗？")]
    //[DisplayName("重建图片元数据")]
    //public async Task<ActionResult<ApiResponse<object>>> RebuildImageMetadata(long id)
    //{
    //    var result = await _imageService.RebuildImageMetadataAsync(id);
    //    return SuccessResponse(result, "图片元数据重建成功");
    //}

    ///// <summary>
    ///// 优化图片存储
    ///// </summary>
    ///// <param name="id">图片ID</param>
    ///// <returns>优化结果</returns>
    //[HttpPost("{id}/optimize")]
    //[Operation("优化存储", "ajax", null, "确定要优化此图片的存储吗？")]
    //[DisplayName("优化图片存储")]
    //public async Task<ActionResult<ApiResponse<object>>> OptimizeImageStorage(long id)
    //{
    //    var result = await _imageService.OptimizeImageStorageAsync(id);
    //    return SuccessResponse(result, "图片存储优化成功");
    //}

    /// <summary>
    /// 获取系统图片统计摘要
    /// </summary>
    /// <returns>图片统计摘要</returns>
    [HttpGet("statistics/summary")]
    [DisplayName("获取图片统计摘要")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageStatisticsSummary()
    {
        var summary = await _imageService.GetImageStatisticsSummaryAsync();
        return SuccessResponse(summary);
    }

    /// <summary>
    /// 获取图片格式分布统计
    /// </summary>
    /// <returns>图片格式分布</returns>
    [HttpGet("statistics/format-distribution")]
    [DisplayName("获取图片格式分布")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageFormatDistribution()
    {
        var distribution = await _imageService.GetImageFormatDistributionAsync();
        return SuccessResponse(distribution);
    }

    /// <summary>
    /// 获取图片尺寸分布统计
    /// </summary>
    /// <returns>图片尺寸分布</returns>
    [HttpGet("statistics/size-distribution")]
    [DisplayName("获取图片尺寸分布")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageSizeDistribution()
    {
        var distribution = await _imageService.GetImageSizeDistributionAsync();
        return SuccessResponse(distribution);
    }

    /// <summary>
    /// 获取图片使用趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>图片使用趋势</returns>
    [HttpGet("statistics/usage-trend")]
    [DisplayName("获取图片使用趋势")]
    public async Task<ActionResult<ApiResponse<object>>> GetImageUsageTrend([FromQuery] int days = 30)
    {
        var trend = await _imageService.GetImageUsageTrendAsync(days);
        return SuccessResponse(trend);
    }

    /// <summary>
    /// 清理过期图片
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup/expired")]
    [Operation("清理过期图片", "ajax", null, "确定要清理所有过期图片吗？此操作不可撤销！", IsBulkOperation = true)]
    [DisplayName("清理过期图片")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupExpiredImages()
    {
        var result = await _imageService.CleanupExpiredImagesAsync();
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 张过期图片");
    }

    /// <summary>
    /// 清理无引用图片
    /// </summary>
    /// <returns>清理结果</returns>
    [HttpPost("cleanup/unreferenced")]
    [Operation("清理无引用图片", "ajax", null, "确定要清理所有无引用的图片吗？此操作不可撤销！", IsBulkOperation = true)]
    [DisplayName("清理无引用图片")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupUnreferencedImages()
    {
        var result = await _imageService.CleanupUnreferencedImagesAsync();
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 张无引用图片");
    }

    /// <summary>
    /// 批量删除图片
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除选中的图片吗？", isBulkOperation: true)]
    [DisplayName("批量删除图片")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchDeleteImages([FromBody] BatchOperationDto<long> request)
    {
        var result = await _imageService.BatchDeleteImagesAsync(request.Ids);
        return SuccessResponse(result, $"批量删除完成！成功删除 {result.Success} 张图片");
    }

    /// <summary>
    /// 批量恢复图片
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>恢复结果</returns>
    [HttpPost("batch/restore")]
    [Operation("批量恢复", "ajax", null, "确定要批量恢复选中的图片吗？", isBulkOperation: true)]
    [DisplayName("批量恢复图片")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchRestoreImages([FromBody] BatchOperationDto<long> request)
    {
        var result = await _imageService.BatchRestoreImagesAsync(request.Ids);
        return SuccessResponse(result, $"批量恢复完成！成功恢复 {result.Success} 张图片");
    }

    /// <summary>
    /// 批量移动图片
    /// </summary>
    /// <param name="request">批量移动请求</param>
    /// <returns>移动结果</returns>
    [HttpPost("batch/move")]
    [Operation("批量移动", "form", null, "确定要将选中的图片移动到指定存储桶吗？", isBulkOperation: true)]
    [DisplayName("批量移动图片")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchMoveImages([FromBody] BatchMoveImagesRequest request)
    {
        var result = await _imageService.BatchMoveImagesAsync(request.ImageIds, request.TargetBucketName);
        return SuccessResponse(result, $"批量移动完成！成功移动 {result.Success} 张图片到存储桶 '{request.TargetBucketName}'");
    }

    /// <summary>
    /// 批量重建图片元数据
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>重建结果</returns>
    [HttpPost("batch/rebuild-metadata")]
    [Operation("批量重建元数据", "ajax", null, "确定要批量重建选中图片的元数据吗？", isBulkOperation: true)]
    [DisplayName("批量重建图片元数据")]
    public async Task<ActionResult<ApiResponse<SystemBatchOperationResult>>> BatchRebuildImageMetadata([FromBody] BatchOperationDto<long> request)
    {
        var result = await _imageService.BatchRebuildImageMetadataAsync(request.Ids);
        return SuccessResponse(result, $"批量重建完成！成功重建 {result.Success} 张图片的元数据");
    }
}

/// <summary>
/// 批量移动图片请求
/// </summary>
public class BatchMoveImagesRequest
{
    /// <summary>
    /// 图片ID列表
    /// </summary>
    [DisplayName("图片ID列表")]
    public List<long> ImageIds { get; set; } = new();

    /// <summary>
    /// 目标存储桶名称
    /// </summary>
    [DisplayName("目标存储桶")]
    public string TargetBucketName { get; set; } = string.Empty;
}
