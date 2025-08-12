namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 存储桶创建选项
/// </summary>
public class BucketCreationOptions
{
    /// <summary>
    /// 访问策略
    /// </summary>
    public BucketAccessPolicy AccessPolicy { get; set; } = BucketAccessPolicy.Private;
    
    /// <summary>
    /// 存储类型
    /// </summary>
    public string StorageClass { get; set; } = "STANDARD";
    
    /// <summary>
    /// 扩展属性
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
