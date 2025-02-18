using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Services;

/// <summary>
/// 配置变更通知器接口
/// </summary>
public interface IConfigChangeNotifier
{
    /// <summary>
    /// 通知配置变更
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    Task NotifyConfigChangedAsync(string appId, string environment);

    /// <summary>
    /// 订阅配置变更
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    /// <param name="callback">回调函数</param>
    Task SubscribeAsync(string appId, string environment, Func<Task> callback);

    /// <summary>
    /// 取消订阅配置变更
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <param name="environment">环境</param>
    Task UnsubscribeAsync(string appId, string environment);
} 