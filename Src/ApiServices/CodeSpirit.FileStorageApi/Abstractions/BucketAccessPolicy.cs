namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 存储桶访问策略
/// </summary>
public enum BucketAccessPolicy
{
    /// <summary>
    /// 私有读写
    /// </summary>
    [Display(Name = "私有读写")]
    Private = 1,
    
    /// <summary>
    /// 公有读私有写
    /// </summary>
    [Display(Name = "公有读私有写")]
    PublicRead = 2,

    /// <summary>
    /// 公有读写
    /// </summary>
    [Display(Name = "公有读写")]
    PublicReadWrite = 3
}