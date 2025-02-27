namespace CodeSpirit.ConfigCenter.Client.SignalR;

/// <summary>
/// 配置中心Hub接口
/// </summary>
public interface IConfigCenterHub
{
    /// <summary>
    /// 注册应用配置监听
    /// </summary>
    Task RegisterAppConfigListener(string appId, string environment);
    
    /// <summary>
    /// 取消注册应用配置监听
    /// </summary>
    Task UnregisterAppConfigListener(string appId, string environment);
} 