namespace CodeSpirit.LLM.Settings;

/// <summary>
/// 设置提供者接口
/// </summary>
public interface ISettingsProvider
{
    /// <summary>
    /// 获取设置
    /// </summary>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="settingsKey">设置键</param>
    /// <returns>设置对象</returns>
    Task<T?> GetSettingsAsync<T>(string settingsKey) where T : class, new();
    
    /// <summary>
    /// 保存设置
    /// </summary>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="settingsKey">设置键</param>
    /// <param name="settings">设置对象</param>
    /// <returns>是否成功</returns>
    Task<bool> SaveSettingsAsync<T>(string settingsKey, T settings) where T : class, new();
}
