using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.Shared.Dtos.Common;
using static CodeSpirit.FileStorageApi.Services.System.ISystemFileService;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统图片服务接口
/// </summary>
public interface ISystemImageService : IScopedDependency
{
    /// <summary>
    /// 获取系统图片列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>图片分页列表</returns>
    Task<PageList<SystemImageDto>> GetSystemImagesAsync(SystemImageQueryDto queryDto);

    /// <summary>
    /// 获取图片详情
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片详情</returns>
    Task<SystemImageDto> GetImageDetailAsync(long id);

    /// <summary>
    /// 获取图片引用信息
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>图片引用信息</returns>
    Task<object> GetImageReferencesAsync(long id);

    /// <summary>
    /// 恢复已删除的图片
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>恢复结果</returns>
    Task RestoreImageAsync(long id);

    /// <summary>
    /// 移动图片到其他存储桶
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>移动结果</returns>
    Task MoveImageAsync(long id, string targetBucketName);

    /// <summary>
    /// 获取图片下载链接
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接信息</returns>
    Task<object> GetImageDownloadUrlAsync(long id, int expirationMinutes);

    /// <summary>
    /// 修复图片数据完整性
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>修复结果</returns>
    Task<FileRepairResult> RepairImageAsync(long id);

    /// <summary>
    /// 获取系统图片统计摘要
    /// </summary>
    /// <returns>图片统计摘要</returns>
    Task<object> GetImageStatisticsSummaryAsync();

    /// <summary>
    /// 获取图片格式分布统计
    /// </summary>
    /// <returns>图片格式分布</returns>
    Task<object> GetImageFormatDistributionAsync();

    /// <summary>
    /// 获取图片尺寸分布统计
    /// </summary>
    /// <returns>图片尺寸分布</returns>
    Task<object> GetImageSizeDistributionAsync();

    /// <summary>
    /// 获取图片使用趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>图片使用趋势</returns>
    Task<object> GetImageUsageTrendAsync(int days);

    /// <summary>
    /// 清理过期图片
    /// </summary>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupExpiredImagesAsync();

    /// <summary>
    /// 清理无引用图片
    /// </summary>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupUnreferencedImagesAsync();

    /// <summary>
    /// 批量删除图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchDeleteImagesAsync(IEnumerable<long> imageIds);

    /// <summary>
    /// 批量恢复图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchRestoreImagesAsync(IEnumerable<long> imageIds);

    /// <summary>
    /// 批量移动图片
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchMoveImagesAsync(IEnumerable<long> imageIds, string targetBucketName);

    /// <summary>
    /// 重建图片元数据
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>重建结果</returns>
    Task<object> RebuildImageMetadataAsync(long id);

    /// <summary>
    /// 批量重建图片元数据
    /// </summary>
    /// <param name="imageIds">图片ID列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchRebuildImageMetadataAsync(IEnumerable<long> imageIds);

    /// <summary>
    /// 优化图片存储
    /// </summary>
    /// <param name="id">图片ID</param>
    /// <returns>优化结果</returns>
    Task<object> OptimizeImageStorageAsync(long id);
}
