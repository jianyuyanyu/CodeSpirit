namespace CodeSpirit.Settings.Services.Interfaces;

/// <summary>
/// 设置管理服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 获取全局设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>设置值</returns>
    Task<string?> GetGlobalSettingAsync(string module, string key);
    
    /// <summary>
    /// 获取全局设置并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetGlobalSettingAsync<T>(string module, string key) where T : class, new();
    
    /// <summary>
    /// 获取全局设置并反序列化为指定类型（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">返回类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetGlobalSettingAsync<T>() where T : class, new();
    
    /// <summary>
    /// 获取模块所有全局设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>设置集合</returns>
    Task<Dictionary<string, string>> GetAllGlobalSettingsAsync(string module);
    
    /// <summary>
    /// 获取用户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="userId">用户ID</param>
    /// <returns>设置值</returns>
    Task<string?> GetUserSettingAsync(string module, string key, string userId);
    
    /// <summary>
    /// 获取用户设置并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="userId">用户ID</param>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetUserSettingAsync<T>(string module, string key, string userId) where T : class, new();
    
    /// <summary>
    /// 获取用户设置并反序列化为指定类型（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">返回类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <param name="userId">用户ID</param>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetUserSettingAsync<T>(string userId) where T : class, new();
    
    /// <summary>
    /// 获取用户所有设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="userId">用户ID</param>
    /// <returns>设置集合</returns>
    Task<Dictionary<string, string>> GetAllUserSettingsAsync(string module, string userId);
    
    /// <summary>
    /// 设置全局设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetGlobalSettingAsync(string module, string key, string value, string? reason = null);
    
    /// <summary>
    /// 设置全局设置对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置对象</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetGlobalSettingAsync<T>(string module, string key, T value, string? reason = null) where T : class;
    
    /// <summary>
    /// 设置全局设置对象（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">对象类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <param name="value">设置对象</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetGlobalSettingAsync<T>(T value, string? reason = null) where T : class;
    
    /// <summary>
    /// 设置用户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetUserSettingAsync(string module, string key, string value, string userId, string? reason = null);
    
    /// <summary>
    /// 设置用户设置对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置对象</param>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetUserSettingAsync<T>(string module, string key, T value, string userId, string? reason = null) where T : class;
    
    /// <summary>
    /// 设置用户设置对象（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">对象类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <param name="value">设置对象</param>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetUserSettingAsync<T>(T value, string userId, string? reason = null) where T : class;
    
    /// <summary>
    /// 批量设置全局设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="settings">设置集合</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> BatchSetGlobalSettingsAsync(string module, Dictionary<string, string> settings, string? reason = null);
    
    /// <summary>
    /// 批量设置用户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="settings">设置集合</param>
    /// <param name="userId">用户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> BatchSetUserSettingsAsync(string module, Dictionary<string, string> settings, string userId, string? reason = null);
    
    /// <summary>
    /// 重置用户设置为全局默认值
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键，为null则重置该模块所有设置</param>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    Task<bool> ResetUserSettingToDefaultAsync(string module, string? key, string userId);
    
    /// <summary>
    /// 获取租户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>设置值</returns>
    Task<string?> GetTenantSettingAsync(string module, string key, string tenantId);
    
    /// <summary>
    /// 获取租户设置并反序列化为指定类型
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetTenantSettingAsync<T>(string module, string key, string tenantId) where T : class, new();
    
    /// <summary>
    /// 获取租户设置并反序列化为指定类型（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">返回类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <param name="tenantId">租户ID</param>
    /// <returns>反序列化后的对象</returns>
    Task<T?> GetTenantSettingAsync<T>(string tenantId) where T : class, new();
    
    /// <summary>
    /// 获取租户所有设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>设置集合</returns>
    Task<Dictionary<string, string>> GetAllTenantSettingsAsync(string module, string tenantId);
    
    /// <summary>
    /// 设置租户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置值</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetTenantSettingAsync(string module, string key, string value, string tenantId, string? reason = null);
    
    /// <summary>
    /// 设置租户设置对象
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <param name="value">设置对象</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetTenantSettingAsync<T>(string module, string key, T value, string tenantId, string? reason = null) where T : class;
    
    /// <summary>
    /// 设置租户设置对象（从 DTO 特性自动获取模块和键）
    /// </summary>
    /// <typeparam name="T">对象类型，必须标记 [SettingsDto] 特性</typeparam>
    /// <param name="value">设置对象</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> SetTenantSettingAsync<T>(T value, string tenantId, string? reason = null) where T : class;
    
    /// <summary>
    /// 批量设置租户设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="settings">设置集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="reason">变更原因</param>
    /// <returns>操作结果</returns>
    Task<bool> BatchSetTenantSettingsAsync(string module, Dictionary<string, string> settings, string tenantId, string? reason = null);
    
    /// <summary>
    /// 重置租户设置为全局默认值
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键，为null则重置该模块所有设置</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>操作结果</returns>
    Task<bool> ResetTenantSettingToDefaultAsync(string module, string? key, string tenantId);
    
    /// <summary>
    /// 获取设置项定义
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>设置项定义</returns>
    Task<SettingItem?> GetSettingDefinitionAsync(string module, string key);
    
    /// <summary>
    /// 获取模块所有设置项定义
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>设置项定义集合</returns>
    Task<List<SettingItem>> GetAllSettingDefinitionsAsync(string module);
    
    /// <summary>
    /// 创建或更新设置项定义
    /// </summary>
    /// <param name="settingItem">设置项</param>
    /// <returns>操作结果</returns>
    Task<bool> CreateOrUpdateSettingDefinitionAsync(SettingItem settingItem);
    
    /// <summary>
    /// 删除设置项定义
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>操作结果</returns>
    Task<bool> DeleteSettingDefinitionAsync(string module, string key);
    
    /// <summary>
    /// 获取设置历史
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="key">设置键</param>
    /// <returns>设置历史记录</returns>
    Task<List<SettingHistory>> GetSettingHistoryAsync(string module, string key);
    
    /// <summary>
    /// 导出设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <returns>设置导出数据</returns>
    Task<string> ExportSettingsAsync(string module);
    
    /// <summary>
    /// 导入设置
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="settingsJson">设置数据</param>
    /// <returns>操作结果</returns>
    Task<bool> ImportSettingsAsync(string module, string settingsJson);
} 