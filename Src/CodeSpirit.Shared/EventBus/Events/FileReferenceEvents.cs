namespace CodeSpirit.Shared.EventBus.Events;

/// <summary>
/// 文件引用操作类型枚举
/// </summary>
public enum FileReferenceOperationType
{
    /// <summary>
    /// 创建
    /// </summary>
    Create,
    
    /// <summary>
    /// 更新
    /// </summary>
    Update,
    
    /// <summary>
    /// 删除
    /// </summary>
    Delete
}

/// <summary>
/// 通用文件引用事件
/// 用于处理任何实体的文件引用创建、更新、删除操作
/// </summary>
public class FileReferenceEvent : TenantAwareEventBase
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体ID（字符串格式，支持long、string等类型）
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体名称
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    public FileReferenceOperationType OperationType { get; set; }
    
    /// <summary>
    /// 文件引用信息列表
    /// </summary>
    public List<FileReferenceInfo> FileReferences { get; set; } = new();
    
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 操作用户ID
    /// </summary>
    public long? OperatorUserId { get; set; }
    
    /// <summary>
    /// 操作用户名称
    /// </summary>
    public string OperatorUserName { get; set; } = string.Empty;
    
    /// <summary>
    /// 附加数据（JSON格式，用于存储特定业务逻辑需要的额外信息）
    /// </summary>
    public string AdditionalData { get; set; } = string.Empty;
}

/// <summary>
/// 文件引用信息
/// </summary>
public class FileReferenceInfo
{
    /// <summary>
    /// 文件ID
    /// </summary>
    public long? FileId { get; set; }
    
    /// <summary>
    /// 文件引用类型（如：Avatar、Logo、Attachment、IdCardPhoto、Banner等）
    /// </summary>
    public string ReferenceType { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件URL
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件名称
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// 文件MIME类型
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    
    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// 是否为主要文件（例如主头像、主Logo等）
    /// </summary>
    public bool IsPrimary { get; set; }
}