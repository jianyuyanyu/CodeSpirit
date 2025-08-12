namespace CodeSpirit.FileStorageApi.Abstractions;

/// <summary>
/// 存储提供程序工厂接口
/// </summary>
public interface IStorageProviderFactory
{
    /// <summary>
    /// 根据提供程序名称获取存储提供程序实例
    /// </summary>
    /// <param name="providerName">提供程序名称</param>
    /// <returns>存储提供程序实例</returns>
    IStorageProvider GetProvider(string providerName);
    
    /// <summary>
    /// 根据提供程序类型获取存储提供程序实例
    /// </summary>
    /// <param name="providerType">提供程序类型</param>
    /// <returns>存储提供程序实例</returns>
    IStorageProvider GetProvider(StorageProviderType providerType);
    
    /// <summary>
    /// 获取所有可用的存储提供程序
    /// </summary>
    /// <returns>存储提供程序列表</returns>
    IEnumerable<IStorageProvider> GetAllProviders();
    
    /// <summary>
    /// 注册存储提供程序
    /// </summary>
    /// <param name="name">提供程序名称</param>
    /// <param name="provider">存储提供程序实例</param>
    void RegisterProvider(string name, IStorageProvider provider);
}
