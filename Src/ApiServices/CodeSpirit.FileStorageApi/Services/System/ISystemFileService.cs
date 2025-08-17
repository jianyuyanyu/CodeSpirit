using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Shared.Dtos.Common;
using static CodeSpirit.FileStorageApi.Services.System.ISystemBucketService;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统文件服务接口
/// </summary>
public interface ISystemFileService : IScopedDependency
{
    /// <summary>
    /// 获取系统文件列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>文件分页列表</returns>
    Task<PageList<SystemFileDto>> GetSystemFilesAsync(SystemFileQueryDto queryDto);

    /// <summary>
    /// 获取文件详情
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件详情</returns>
    Task<SystemFileDto> GetFileDetailAsync(long id);

    /// <summary>
    /// 获取文件引用信息
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>文件引用信息</returns>
    Task<object> GetFileReferencesAsync(long id);

    /// <summary>
    /// 强制删除文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>任务</returns>
    Task ForceDeleteFileAsync(long id);

    /// <summary>
    /// 恢复文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>任务</returns>
    Task RestoreFileAsync(long id);

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>任务</returns>
    Task MoveFileAsync(long id, string targetBucketName);

    /// <summary>
    /// 获取文件下载链接
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <param name="expirationMinutes">过期时间（分钟）</param>
    /// <returns>下载链接信息</returns>
    Task<object> GetFileDownloadUrlAsync(long id, int expirationMinutes);

    /// <summary>
    /// 修复文件
    /// </summary>
    /// <param name="id">文件ID</param>
    /// <returns>修复结果</returns>
    Task<FileRepairResult> RepairFileAsync(long id);

    /// <summary>
    /// 获取文件统计摘要
    /// </summary>
    /// <returns>统计摘要</returns>
    Task<object> GetFileStatisticsSummaryAsync();

    /// <summary>
    /// 获取文件类型分布
    /// </summary>
    /// <returns>文件类型分布</returns>
    Task<object> GetFileTypeDistributionAsync();

    /// <summary>
    /// 获取存储使用趋势
    /// </summary>
    /// <param name="days">统计天数</param>
    /// <returns>存储使用趋势</returns>
    Task<object> GetStorageUsageTrendAsync(int days);

    /// <summary>
    /// 清理过期文件
    /// </summary>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupExpiredFilesAsync();

    /// <summary>
    /// 清理无引用文件
    /// </summary>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupUnreferencedFilesAsync();

    /// <summary>
    /// 批量删除文件
    /// </summary>
    /// <param name="fileIds">文件ID列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchDeleteFilesAsync(IEnumerable<long> fileIds);

    /// <summary>
    /// 批量恢复文件
    /// </summary>
    /// <param name="fileIds">文件ID列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchRestoreFilesAsync(IEnumerable<long> fileIds);

    /// <summary>
    /// 批量移动文件
    /// </summary>
    /// <param name="fileIds">文件ID列表</param>
    /// <param name="targetBucketName">目标存储桶名称</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchMoveFilesAsync(IEnumerable<long> fileIds, string targetBucketName);
}

/// <summary>
/// 文件修复结果
/// </summary>
public class FileRepairResult
{
    /// <summary>
    /// 是否已修复
    /// </summary>
    public bool IsRepaired { get; set; }

    /// <summary>
    /// 修复详情
    /// </summary>
    public List<string> RepairDetails { get; set; } = new();

    /// <summary>
    /// 修复时间
    /// </summary>
    public DateTime RepairTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}


