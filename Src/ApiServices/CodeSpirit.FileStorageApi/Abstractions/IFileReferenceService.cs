using CodeSpirit.FileStorageApi.Entities;
using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Core.Dtos;

namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 文件引用服务接口
/// 管理文件的引用关系，支持引用计数和生命周期管理
/// </summary>
public interface IFileReferenceService : IScopedDependency
{
    /// <summary>
    /// 创建文件引用
    /// </summary>
    /// <param name="request">引用创建请求</param>
    /// <returns>引用ID</returns>
    Task<long> CreateReferenceAsync(FileReferenceCreateRequest request);
    
    /// <summary>
    /// 确认文件引用
    /// </summary>
    /// <param name="referenceId">引用ID</param>
    /// <returns>确认结果</returns>
    Task<bool> ConfirmReferenceAsync(long referenceId);
    
    /// <summary>
    /// 取消文件引用
    /// </summary>
    /// <param name="referenceId">引用ID</param>
    /// <returns>取消结果</returns>
    Task<bool> CancelReferenceAsync(long referenceId);
    
    /// <summary>
    /// 批量确认文件引用
    /// </summary>
    /// <param name="referenceIds">引用ID列表</param>
    /// <returns>确认结果</returns>
    Task<BatchOperationResult> BatchConfirmReferencesAsync(IEnumerable<long> referenceIds);
    
    /// <summary>
    /// 根据来源删除引用
    /// </summary>
    /// <param name="sourceService">来源服务</param>
    /// <param name="sourceEntityType">来源实体类型</param>
    /// <param name="sourceEntityId">来源实体ID</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteReferencesBySourceAsync(string sourceService, string sourceEntityType, string sourceEntityId);
    
    /// <summary>
    /// 获取文件的引用列表
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <returns>引用列表</returns>
    Task<IEnumerable<FileReferenceEntity>> GetFileReferencesAsync(long fileId);
    
    /// <summary>
    /// 获取引用详情
    /// </summary>
    /// <param name="referenceId">引用ID</param>
    /// <returns>引用详情</returns>
    Task<FileReferenceEntity?> GetReferenceAsync(long referenceId);
    
    /// <summary>
    /// 查询文件引用
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>引用列表</returns>
    Task<PageList<FileReferenceEntity>> QueryReferencesAsync(ReferenceQueryRequest request);
    
    /// <summary>
    /// 清理过期的临时引用
    /// </summary>
    /// <returns>清理数量</returns>
    Task<int> CleanupExpiredReferencesAsync();
    
    /// <summary>
    /// 获取无引用的文件
    /// </summary>
    /// <param name="olderThan">早于指定时间</param>
    /// <returns>无引用的文件ID列表</returns>
    Task<IEnumerable<long>> GetUnreferencedFilesAsync(DateTime olderThan);
}

/// <summary>
/// 文件引用创建请求
/// </summary>
public class FileReferenceCreateRequest
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long FileId { get; set; }
    
    /// <summary>
    /// 引用来源服务
    /// </summary>
    public string SourceService { get; set; }
    
    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    public string SourceEntityType { get; set; }
    
    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    public string SourceEntityId { get; set; }
    
    /// <summary>
    /// 引用字段名
    /// </summary>
    public string FieldName { get; set; }
    
    /// <summary>
    /// 引用类型
    /// </summary>
    public ReferenceType ReferenceType { get; set; }
    
    /// <summary>
    /// 是否为临时引用
    /// </summary>
    public bool IsTemporary { get; set; }
    
    /// <summary>
    /// 引用过期时间（临时引用）
    /// </summary>
    public DateTime? ExpirationTime { get; set; }
    
    /// <summary>
    /// 引用备注
    /// </summary>
    public string Remarks { get; set; }
    
    /// <summary>
    /// 扩展属性
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// 引用查询请求
/// </summary>
public class ReferenceQueryRequest
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long? FileId { get; set; }
    
    /// <summary>
    /// 引用来源服务
    /// </summary>
    public string SourceService { get; set; }
    
    /// <summary>
    /// 引用来源实体类型
    /// </summary>
    public string SourceEntityType { get; set; }
    
    /// <summary>
    /// 引用来源实体ID
    /// </summary>
    public string SourceEntityId { get; set; }
    
    /// <summary>
    /// 引用状态
    /// </summary>
    public ReferenceStatus? Status { get; set; }
    
    /// <summary>
    /// 引用类型
    /// </summary>
    public ReferenceType? ReferenceType { get; set; }
    
    /// <summary>
    /// 是否临时引用
    /// </summary>
    public bool? IsTemporary { get; set; }
    
    /// <summary>
    /// 创建开始时间
    /// </summary>
    public DateTime? CreatedFrom { get; set; }
    
    /// <summary>
    /// 创建结束时间
    /// </summary>
    public DateTime? CreatedTo { get; set; }
    
    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// 排序字段
    /// </summary>
    public string OrderBy { get; set; } = "CreatedTime";
    
    /// <summary>
    /// 是否降序
    /// </summary>
    public bool Descending { get; set; } = true;
}
