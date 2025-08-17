using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.Shared.Dtos.Common;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统租户存储服务接口
/// </summary>
public interface ISystemTenantStorageService : IScopedDependency
{
    /// <summary>
    /// 获取租户存储统计列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>租户存储统计分页列表</returns>
    Task<PageList<TenantStorageStatisticsDto>> GetTenantStorageStatisticsAsync(QueryDtoBase queryDto);

    /// <summary>
    /// 获取租户存储详情
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>租户存储详情</returns>
    Task<TenantStorageStatisticsDto> GetTenantStorageDetailAsync(string tenantId);

    /// <summary>
    /// 获取系统总存储统计
    /// </summary>
    /// <returns>系统总存储统计</returns>
    Task<object> GetSystemStorageSummaryAsync();

    /// <summary>
    /// 清理租户无效文件
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupTenantInvalidFilesAsync(string tenantId);

    /// <summary>
    /// 刷新租户存储统计
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns>任务</returns>
    Task RefreshTenantStorageStatisticsAsync(string tenantId);

    /// <summary>
    /// 批量清理无效文件
    /// </summary>
    /// <param name="tenantIds">租户ID列表</param>
    /// <returns>批量清理结果</returns>
    Task<BatchCleanupResult> BatchCleanupInvalidFilesAsync(IEnumerable<string> tenantIds);
}

/// <summary>
/// 清理结果
/// </summary>
public class CleanupResult
{
    /// <summary>
    /// 清理的文件数量
    /// </summary>
    public int CleanedFilesCount { get; set; }

    /// <summary>
    /// 释放的存储空间（字节）
    /// </summary>
    public long ReleasedSpace { get; set; }

    /// <summary>
    /// 释放的存储空间（格式化显示）
    /// </summary>
    public string ReleasedSpaceFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 清理详情
    /// </summary>
    public List<string> Details { get; set; } = new();
}

/// <summary>
/// 批量清理结果
/// </summary>
public class BatchCleanupResult
{
    /// <summary>
    /// 处理的租户数量
    /// </summary>
    public int ProcessedTenantsCount { get; set; }

    /// <summary>
    /// 清理的文件总数
    /// </summary>
    public int TotalCleanedFilesCount { get; set; }

    /// <summary>
    /// 释放的存储空间总计（字节）
    /// </summary>
    public long TotalReleasedSpace { get; set; }

    /// <summary>
    /// 释放的存储空间总计（格式化显示）
    /// </summary>
    public string TotalReleasedSpaceFormatted { get; set; } = string.Empty;

    /// <summary>
    /// 各租户清理详情
    /// </summary>
    public List<TenantCleanupDetail> TenantDetails { get; set; } = new();
}

/// <summary>
/// 租户清理详情
/// </summary>
public class TenantCleanupDetail
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 清理的文件数量
    /// </summary>
    public int CleanedFilesCount { get; set; }

    /// <summary>
    /// 释放的存储空间
    /// </summary>
    public long ReleasedSpace { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
