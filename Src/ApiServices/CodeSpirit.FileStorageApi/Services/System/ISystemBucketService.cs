using CodeSpirit.Core.Dtos;
using CodeSpirit.FileStorageApi.Dtos.System;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.Shared.Dtos.Common;

namespace CodeSpirit.FileStorageApi.Services.System;

/// <summary>
/// 系统存储桶服务接口
/// </summary>
public interface ISystemBucketService
{
    /// <summary>
    /// 获取系统存储桶列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>存储桶分页列表</returns>
    Task<PageList<SystemBucketDto>> GetSystemBucketsAsync(QueryDtoBase queryDto);

    /// <summary>
    /// 获取存储桶详情
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>存储桶详情</returns>
    Task<SystemBucketDto> GetBucketDetailAsync(string bucketName);

    /// <summary>
    /// 获取存储桶统计信息
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>统计信息</returns>
    Task<object> GetBucketStatisticsAsync(string bucketName);

    /// <summary>
    /// 启用存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>任务</returns>
    Task EnableBucketAsync(string bucketName);

    /// <summary>
    /// 禁用存储桶
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>任务</returns>
    Task DisableBucketAsync(string bucketName);

    /// <summary>
    /// 清理存储桶无效文件
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>清理结果</returns>
    Task<CleanupResult> CleanupBucketInvalidFilesAsync(string bucketName);

    /// <summary>
    /// 刷新存储桶统计
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>任务</returns>
    Task RefreshBucketStatisticsAsync(string bucketName);

    /// <summary>
    /// 测试存储桶连接
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>测试结果</returns>
    Task<BucketConnectionTestResult> TestBucketConnectionAsync(string bucketName);

    /// <summary>
    /// 获取存储桶配置
    /// </summary>
    /// <param name="bucketName">存储桶名称</param>
    /// <returns>存储桶配置</returns>
    Task<object> GetBucketConfigurationAsync(string bucketName);

    /// <summary>
    /// 批量启用存储桶
    /// </summary>
    /// <param name="bucketNames">存储桶名称列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchEnableBucketsAsync(IEnumerable<string> bucketNames);

    /// <summary>
    /// 批量禁用存储桶
    /// </summary>
    /// <param name="bucketNames">存储桶名称列表</param>
    /// <returns>批量操作结果</returns>
    Task<SystemBatchOperationResult> BatchDisableBucketsAsync(IEnumerable<string> bucketNames);
}

    /// <summary>
    /// 系统服务批量操作结果
    /// </summary>
    public class SystemBatchOperationResult : BatchOperationResult
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 操作详情
        /// </summary>
        public List<SystemBatchOperationDetail> Details { get; set; } = new();
    }
    
    /// <summary>
    /// 系统批量操作详情
    /// </summary>
    public class SystemBatchOperationDetail
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public string ItemId { get; set; }
        
        /// <summary>
        /// 状态
        /// </summary>
        public SystemBatchOperationStatus Status { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// 系统批量操作状态
    /// </summary>
    public enum SystemBatchOperationStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,
        
        /// <summary>
        /// 失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 存储桶连接测试结果
    /// </summary>
    public class BucketConnectionTestResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 测试详情
    /// </summary>
    public List<string> TestDetails { get; set; } = new();

    /// <summary>
    /// 测试时间
    /// </summary>
    public DateTime TestTime { get; set; } = DateTime.UtcNow;
}
