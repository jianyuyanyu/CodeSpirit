using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Services.System;
using CodeSpirit.Shared.Dtos.Common;
using static CodeSpirit.FileStorageApi.Services.System.ISystemBucketService;
using static CodeSpirit.FileStorageApi.Services.System.ISystemTenantStorageService;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers.System;

/// <summary>
/// 系统平台存储桶管理控制器
/// </summary>
[DisplayName("存储桶管理")]
[Navigation(Icon = "fa-solid fa-box-archive", PlatformType = PlatformType.System)]
public class SystemBucketsController : ApiControllerBase
{
    private readonly ISystemBucketService _bucketService;
    private readonly ILogger<SystemBucketsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bucketService">存储桶服务</param>
    /// <param name="logger">日志服务</param>
    public SystemBucketsController(
        ISystemBucketService bucketService,
        ILogger<SystemBucketsController> logger)
    {
        _bucketService = bucketService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统存储桶列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>存储桶分页列表</returns>
    [HttpGet]
    [DisplayName("获取存储桶列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemBucketDto>>>> GetSystemBuckets([FromQuery] QueryDtoBase queryDto)
    {
        PageList<SystemBucketDto> buckets = await _bucketService.GetSystemBucketsAsync(queryDto);
        return SuccessResponse(buckets);
    }

    /// <summary>
    /// 导出存储桶列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>存储桶数据</returns>
    [HttpGet("export")]
    [DisplayName("导出存储桶列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemBucketDto>>>> ExportSystemBuckets([FromQuery] QueryDtoBase queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 10000;
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取存储桶数据
        PageList<SystemBucketDto> buckets = await _bucketService.GetSystemBucketsAsync(queryDto);

        // 如果数据为空则返回错误信息
        return buckets.Items.Count == 0 ? BadResponse<PageList<SystemBucketDto>>("没有数据可供导出") : SuccessResponse(buckets);
    }

    /// <summary>
    /// 获取存储桶详情
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>存储桶详情</returns>
    [HttpGet("{bucketName}")]
    [DisplayName("获取存储桶详情")]
    public async Task<ActionResult<ApiResponse<SystemBucketDto>>> GetBucketDetail(string bucketName)
    {
        SystemBucketDto bucket = await _bucketService.GetBucketDetailAsync(bucketName);
        return SuccessResponse(bucket);
    }

    /// <summary>
    /// 获取存储桶使用统计
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>使用统计</returns>
    [HttpGet("{bucketName}/statistics")]
    [DisplayName("获取存储桶统计")]
    public async Task<ActionResult<ApiResponse<object>>> GetBucketStatistics(string bucketName)
    {
        var statistics = await _bucketService.GetBucketStatisticsAsync(bucketName);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 启用存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>操作结果</returns>
    [HttpPost("{bucketName}/enable")]
    [Operation("启用", "ajax", null, "确定要启用此存储桶吗？")]
    [DisplayName("启用存储桶")]
    public async Task<ActionResult<ApiResponse>> EnableBucket(string bucketName)
    {
        await _bucketService.EnableBucketAsync(bucketName);
        return SuccessResponse($"存储桶 '{bucketName}' 已启用");
    }

    /// <summary>
    /// 禁用存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>操作结果</returns>
    [HttpPost("{bucketName}/disable")]
    [Operation("禁用", "ajax", null, "确定要禁用此存储桶吗？禁用后新文件将无法上传到此存储桶！")]
    [DisplayName("禁用存储桶")]
    public async Task<ActionResult<ApiResponse>> DisableBucket(string bucketName)
    {
        await _bucketService.DisableBucketAsync(bucketName);
        return SuccessResponse($"存储桶 '{bucketName}' 已禁用");
    }

    /// <summary>
    /// 清理存储桶无效文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>清理结果</returns>
    [HttpPost("{bucketName}/cleanup")]
    [Operation("清理无效文件", "ajax", null, "确定要清理此存储桶的无效文件吗？此操作不可撤销！")]
    [DisplayName("清理存储桶无效文件")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupBucketInvalidFiles(string bucketName)
    {
        var result = await _bucketService.CleanupBucketInvalidFilesAsync(bucketName);
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 个无效文件，释放存储空间 {result.ReleasedSpaceFormatted}");
    }

    /// <summary>
    /// 刷新存储桶统计
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>刷新结果</returns>
    [HttpPost("{bucketName}/refresh-statistics")]
    [Operation("刷新统计", "ajax", null, "确定要重新计算此存储桶的统计信息吗？")]
    [DisplayName("刷新存储桶统计")]
    public async Task<ActionResult<ApiResponse>> RefreshBucketStatistics(string bucketName)
    {
        await _bucketService.RefreshBucketStatisticsAsync(bucketName);
        return SuccessResponse($"存储桶 '{bucketName}' 统计信息已刷新完成");
    }

    /// <summary>
    /// 测试存储桶连接
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>测试结果</returns>
    [HttpPost("{bucketName}/test-connection")]
    [Operation("测试连接", "ajax", null, "确定要测试此存储桶的连接吗？")]
    [DisplayName("测试存储桶连接")]
    public async Task<ActionResult<ApiResponse<BucketConnectionTestResult>>> TestBucketConnection(string bucketName)
    {
        var result = await _bucketService.TestBucketConnectionAsync(bucketName);
        return SuccessResponse(result, result.IsSuccess ? "连接测试成功" : "连接测试失败");
    }

    /// <summary>
    /// 获取存储桶配置
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>存储桶配置</returns>
    [HttpGet("{bucketName}/configuration")]
    [DisplayName("获取存储桶配置")]
    public async Task<ActionResult<ApiResponse<object>>> GetBucketConfiguration(string bucketName)
    {
        var configuration = await _bucketService.GetBucketConfigurationAsync(bucketName);
        return SuccessResponse(configuration);
    }

    /// <summary>
    /// 批量启用存储桶
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/enable")]
    [Operation("批量启用", "ajax", null, "确定要批量启用选中的存储桶吗？", isBulkOperation: true)]
    [DisplayName("批量启用存储桶")]
    public async Task<ActionResult<ApiResponse>> BatchEnableBuckets([FromBody] BatchOperationDto<string> request)
    {
        var result = await _bucketService.BatchEnableBucketsAsync(request.Ids);
        return SuccessResponse($"批量启用完成！成功启用 {result.Success} 个存储桶");
    }

    /// <summary>
    /// 批量禁用存储桶
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/disable")]
    [Operation("批量禁用", "ajax", null, "确定要批量禁用选中的存储桶吗？", isBulkOperation: true)]
    [DisplayName("批量禁用存储桶")]
    public async Task<ActionResult<ApiResponse>> BatchDisableBuckets([FromBody] BatchOperationDto<string> request)
    {
        var result = await _bucketService.BatchDisableBucketsAsync(request.Ids);
        return SuccessResponse($"批量禁用完成！成功禁用 {result.Success} 个存储桶");
    }
}
