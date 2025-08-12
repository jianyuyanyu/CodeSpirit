using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Services.System;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.FileStorageApi.Controllers.System;

/// <summary>
/// 系统平台租户存储管理控制器
/// </summary>
[DisplayName("租户存储")]
[Navigation(Icon = "fa-solid fa-database", PlatformType = PlatformType.System)]
public class SystemTenantStorageController : ApiControllerBase
{
    private readonly ISystemTenantStorageService _tenantStorageService;
    private readonly ILogger<SystemTenantStorageController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantStorageService">租户存储服务</param>
    /// <param name="logger">日志服务</param>
    public SystemTenantStorageController(
        ISystemTenantStorageService tenantStorageService,
        ILogger<SystemTenantStorageController> logger)
    {
        _tenantStorageService = tenantStorageService;
        _logger = logger;
    }

    /// <summary>
    /// 获取租户存储统计列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>租户存储统计分页列表</returns>
    [HttpGet]
    [DisplayName("获取租户存储列表")]
    public async Task<ActionResult<ApiResponse<PageList<TenantStorageStatisticsDto>>>> GetTenantStorageStatistics([FromQuery] QueryDtoBase queryDto)
    {
        PageList<TenantStorageStatisticsDto> statistics = await _tenantStorageService.GetTenantStorageStatisticsAsync(queryDto);
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 导出租户存储统计列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>租户存储统计数据</returns>
    [HttpGet("export")]
    [DisplayName("导出租户存储列表")]
    public async Task<ActionResult<ApiResponse<PageList<TenantStorageStatisticsDto>>>> ExportTenantStorageStatistics([FromQuery] QueryDtoBase queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 50000; // 系统级导出数量上限更高
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取租户存储统计数据
        PageList<TenantStorageStatisticsDto> statistics = await _tenantStorageService.GetTenantStorageStatisticsAsync(queryDto);

        // 如果数据为空则返回错误信息
        return statistics.Items.Count == 0 ? BadResponse<PageList<TenantStorageStatisticsDto>>("没有数据可供导出") : SuccessResponse(statistics);
    }

    /// <summary>
    /// 获取租户存储详情
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户存储详情</returns>
    [HttpGet("{tenantId}")]
    [DisplayName("获取租户存储详情")]
    public async Task<ActionResult<ApiResponse<TenantStorageStatisticsDto>>> GetTenantStorageDetail(string tenantId)
    {
        TenantStorageStatisticsDto tenantStorage = await _tenantStorageService.GetTenantStorageDetailAsync(tenantId);
        return SuccessResponse(tenantStorage);
    }

    /// <summary>
    /// 获取系统总存储统计
    /// </summary>
    /// <returns>系统总存储统计</returns>
    [HttpGet("system/summary")]
    [DisplayName("获取系统存储摘要")]
    public async Task<ActionResult<ApiResponse<object>>> GetSystemStorageSummary()
    {
        var summary = await _tenantStorageService.GetSystemStorageSummaryAsync();
        return SuccessResponse(summary);
    }

    /// <summary>
    /// 清理租户无效文件
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>清理结果</returns>
    [HttpPost("{tenantId}/cleanup")]
    [Operation("清理无效文件", "ajax", null, "确定要清理该租户的无效文件吗？此操作不可撤销！")]
    [DisplayName("清理租户无效文件")]
    public async Task<ActionResult<ApiResponse<CleanupResult>>> CleanupTenantInvalidFiles(string tenantId)
    {
        var result = await _tenantStorageService.CleanupTenantInvalidFilesAsync(tenantId);
        return SuccessResponse(result, $"清理完成！共清理 {result.CleanedFilesCount} 个无效文件，释放存储空间 {result.ReleasedSpaceFormatted}");
    }

    /// <summary>
    /// 刷新租户存储统计
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>刷新结果</returns>
    [HttpPost("{tenantId}/refresh-statistics")]
    [Operation("刷新统计", "ajax", null, "确定要重新计算该租户的存储统计吗？")]
    [DisplayName("刷新租户存储统计")]
    public async Task<ActionResult<ApiResponse>> RefreshTenantStorageStatistics(string tenantId)
    {
        await _tenantStorageService.RefreshTenantStorageStatisticsAsync(tenantId);
        return SuccessResponse("租户存储统计已刷新完成");
    }

    /// <summary>
    /// 批量清理多个租户无效文件
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>清理结果</returns>
    [HttpPost("batch/cleanup")]
    [Operation("批量清理", "ajax", null, "确定要批量清理选中租户的无效文件吗？", isBulkOperation: true)]
    [DisplayName("批量清理无效文件")]
    public async Task<ActionResult<ApiResponse<BatchCleanupResult>>> BatchCleanupInvalidFiles([FromBody] BatchOperationDto<string> request)
    {
        var result = await _tenantStorageService.BatchCleanupInvalidFilesAsync(request.Ids);
        return SuccessResponse(result, $"批量清理完成！共处理 {request.Ids.Count} 个租户，清理 {result.TotalCleanedFilesCount} 个无效文件");
    }
}
