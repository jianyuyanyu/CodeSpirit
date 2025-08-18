using Microsoft.EntityFrameworkCore;
using CodeSpirit.Settings.Data;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace CodeSpirit.Settings.Services.Implementations;

/// <summary>
/// 设置管理服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly SettingsDbContext _context;
    private readonly ILogger<SettingsService> _logger;
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;
    
    // 用于跟踪模块相关的缓存键
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _moduleKeysMap = new();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">设置数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cache">分布式缓存</param>
    public SettingsService(SettingsDbContext context, ILogger<SettingsService> logger, IDistributedCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        
        // 设置默认缓存选项（10分钟过期）
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
    }
    
    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <param name="keyParts">键组成部分</param>
    /// <returns>缓存键</returns>
    protected virtual string GenerateCacheKey(params string[] keyParts)
    {
        var cacheKey = $"Settings:{string.Join(":", keyParts)}";
        
        // 跟踪模块关联的缓存键
        if (keyParts.Length > 0 && !string.IsNullOrEmpty(keyParts[1]))
        {
            string module = keyParts[1]; // 第二个参数应该是模块名
            _moduleKeysMap.GetOrAdd(module, new ConcurrentBag<string>()).Add(cacheKey);
        }
        
        return cacheKey;
    }
    
    /// <summary>
    /// 从缓存获取值
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="cacheKey">缓存键</param>
    /// <returns>缓存值，无则返回默认值</returns>
    private async Task<T?> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        try
        {
            var cachedBytes = await _cache.GetAsync(cacheKey);
            if (cachedBytes != null && cachedBytes.Length > 0)
            {
                var cachedValue = Encoding.UTF8.GetString(cachedBytes);
                return JsonConvert.DeserializeObject<T>(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从缓存读取值时出错: {CacheKey}", cacheKey);
        }
        
        return null;
    }
    
    /// <summary>
    /// 设置缓存值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="cacheKey">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="options">缓存选项</param>
    private async Task SetCacheAsync<T>(string cacheKey, T value, DistributedCacheEntryOptions? options = null)
    {
        try
        {
            var valueJson = JsonConvert.SerializeObject(value);
            var valueBytes = Encoding.UTF8.GetBytes(valueJson);
            
            await _cache.SetAsync(cacheKey, valueBytes, options ?? _cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置缓存值时出错: {CacheKey}", cacheKey);
        }
    }
    
    /// <summary>
    /// 移除缓存
    /// </summary>
    /// <param name="cacheKey">缓存键</param>
    private async Task RemoveCacheAsync(string cacheKey)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "移除缓存值时出错: {CacheKey}", cacheKey);
        }
    }
    
    /// <summary>
    /// 移除模块相关的所有缓存
    /// </summary>
    /// <param name="module">模块名</param>
    private async Task RemoveModuleCachesAsync(string module)
    {
        try
        {
            _logger.LogInformation("移除模块相关缓存: {Module}", module);
            
            // 检查是否有该模块的键映射
            if (_moduleKeysMap.TryGetValue(module, out var keys))
            {
                // 创建一个新的集合来存储需要移除的键
                var keysToRemove = new List<string>();
                
                // 添加所有已知的键
                foreach (var key in keys)
                {
                    keysToRemove.Add(key);
                }
                
                // 批量移除缓存
                foreach (var key in keysToRemove)
                {
                    await RemoveCacheAsync(key);
                }
                
                // 清空该模块的键映射
                _moduleKeysMap.TryRemove(module, out _);
                _moduleKeysMap.TryAdd(module, new ConcurrentBag<string>());
                
                _logger.LogInformation("已移除{Count}个与模块{Module}相关的缓存项", keysToRemove.Count, module);
            }
            else
            {
                _logger.LogInformation("未找到与模块{Module}相关的缓存项", module);
            }
            
            // 创建强制模式匹配的缓存键，用于确保清除常见的模式
            var commonPatterns = new[]
            {
                GenerateCacheKey("Global", module, "*"),
                GenerateCacheKey("GlobalObj", module, "*"),
                GenerateCacheKey("AllGlobal", module),
                GenerateCacheKey("User", module, "*"),
                GenerateCacheKey("UserObj", module, "*"),
                GenerateCacheKey("AllUser", module, "*"),
                GenerateCacheKey("Definition", module, "*"),
                GenerateCacheKey("AllDefinitions", module),
                GenerateCacheKey("History", module, "*")
            };
            
            // 查询该模块的所有设置项
            var settingItems = await _context.SettingItems
                .Where(s => s.Module == module)
                .ToListAsync();
            
            // 为每个设置项创建具体的缓存键并清除
            foreach (var item in settingItems)
            {
                // 全局设置相关缓存
                if (item.Scope == SettingScope.Global)
                {
                    await RemoveCacheAsync(GenerateCacheKey("Global", module, item.Key));
                    await RemoveCacheAsync(GenerateCacheKey("GlobalObj", module, item.Key));
                    await RemoveCacheAsync(GenerateCacheKey("Definition", module, item.Key));
                    await RemoveCacheAsync(GenerateCacheKey("History", module, item.Key));
                }
                // 用户设置相关缓存
                else if (item.Scope == SettingScope.User && !string.IsNullOrEmpty(item.ScopeId))
                {
                    await RemoveCacheAsync(GenerateCacheKey("User", module, item.Key, item.ScopeId));
                    await RemoveCacheAsync(GenerateCacheKey("UserObj", module, item.Key, item.ScopeId));
                    await RemoveCacheAsync(GenerateCacheKey("AllUser", module, item.ScopeId));
                }
            }
            
            // 移除通用缓存
            await RemoveCacheAsync(GenerateCacheKey("AllGlobal", module));
            await RemoveCacheAsync(GenerateCacheKey("AllDefinitions", module));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除模块相关缓存时出错: {Module}", module);
        }
    }
    
    /// <inheritdoc/>
    public async Task<string?> GetGlobalSettingAsync(string module, string key)
    {
        var cacheKey = GenerateCacheKey("Global", module, key);
        var cachedValue = await GetFromCacheAsync<string>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var setting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
        
        if (setting != null)
        {
            await SetCacheAsync(cacheKey, setting.Value);
            return setting.Value;
        }
        
        return null;
    }
    
    /// <inheritdoc/>
    public async Task<T?> GetGlobalSettingAsync<T>(string module, string key) where T : class, new()
    {
        var cacheKey = GenerateCacheKey("GlobalObj", module, key, typeof(T).Name);
        var cachedValue = await GetFromCacheAsync<T>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var settingValue = await GetGlobalSettingAsync(module, key);
        
        if (string.IsNullOrEmpty(settingValue))
        {
            return null;
        }
        
        try
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(settingValue);
            if (result != null)
            {
                await SetCacheAsync(cacheKey, result);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化设置值时出错: {Module}, {Key}", module, key);
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetAllGlobalSettingsAsync(string module)
    {
        var cacheKey = GenerateCacheKey("AllGlobal", module);
        var cachedSettings = await GetFromCacheAsync<Dictionary<string, string>>(cacheKey);
        
        if (cachedSettings != null)
        {
            return cachedSettings;
        }
        
        var settings = await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.Global)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        
        await SetCacheAsync(cacheKey, settings);
        return settings;
    }
    
    /// <inheritdoc/>
    public async Task<string?> GetUserSettingAsync(string module, string key, string userId)
    {
        var cacheKey = GenerateCacheKey("User", module, key, userId);
        var cachedValue = await GetFromCacheAsync<string>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        // 先查询用户特定设置
        var userSetting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.User && s.ScopeId == userId)
            .FirstOrDefaultAsync();
            
        if (userSetting != null)
        {
            await SetCacheAsync(cacheKey, userSetting.Value);
            return userSetting.Value;
        }
        
        // 如果用户没有特定设置，返回全局设置
        return await GetGlobalSettingAsync(module, key);
    }
    
    /// <inheritdoc/>
    public async Task<T?> GetUserSettingAsync<T>(string module, string key, string userId) where T : class, new()
    {
        var cacheKey = GenerateCacheKey("UserObj", module, key, userId, typeof(T).Name);
        var cachedValue = await GetFromCacheAsync<T>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var settingValue = await GetUserSettingAsync(module, key, userId);
        
        if (string.IsNullOrEmpty(settingValue))
        {
            return null;
        }
        
        try
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(settingValue);
            if (result != null)
            {
                await SetCacheAsync(cacheKey, result);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化用户设置值时出错: {Module}, {Key}, {UserId}", module, key, userId);
            return null;
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetAllUserSettingsAsync(string module, string userId)
    {
        var cacheKey = GenerateCacheKey("AllUser", module, userId);
        var cachedSettings = await GetFromCacheAsync<Dictionary<string, string>>(cacheKey);
        
        if (cachedSettings != null)
        {
            return cachedSettings;
        }
        
        // 获取全局设置
        var globalSettings = await GetAllGlobalSettingsAsync(module);
        
        // 获取用户特定设置
        var userSettings = await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.User && s.ScopeId == userId)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
            
        // 合并设置（用户设置优先）
        foreach (var key in userSettings.Keys)
        {
            globalSettings[key] = userSettings[key];
        }
        
        await SetCacheAsync(cacheKey, globalSettings);
        return globalSettings;
    }
    
    /// <inheritdoc/>
    public async Task<bool> SetGlobalSettingAsync(string module, string key, string value, string? reason = null)
    {
        try
        {
            var setting = await _context.SettingItems
                .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
                .FirstOrDefaultAsync();
                
            if (setting == null)
            {
                // 创建新设置
                setting = new SettingItem
                {
                    Module = module,
                    Key = key,
                    Value = value,
                    Name = key, // 默认使用键作为名称
                    Scope = SettingScope.Global,
                    Version = 1
                };
                
                _context.SettingItems.Add(setting);
            }
            else
            {
                // 记录历史
                await CreateHistoryAsync(setting, value, reason);
                
                // 更新设置
                setting.Value = value;
                setting.Version++;
            }
            
            await _context.SaveChangesAsync();
            
            // 更新缓存
            var cacheKey = GenerateCacheKey("Global", module, key);
            await SetCacheAsync(cacheKey, value);
            
            // 移除相关联的缓存
            await RemoveCacheAsync(GenerateCacheKey("AllGlobal", module));
            await RemoveCacheAsync(GenerateCacheKey("GlobalObj", module, key));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置全局设置时出错: {Module}, {Key}", module, key);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> SetGlobalSettingAsync<T>(string module, string key, T value, string? reason = null) where T : class
    {
        try
        {
            string jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
            
            var setting = await _context.SettingItems
                .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
                .FirstOrDefaultAsync();
                
            if (setting == null)
            {
                // 创建新设置
                setting = new SettingItem
                {
                    Module = module,
                    Key = key,
                    Value = jsonValue,
                    Name = key, // 默认使用键作为名称
                    Scope = SettingScope.Global,
                    ValueType = SettingValueType.Json, // 设置值类型为JSON
                    Version = 1
                };
                
                _context.SettingItems.Add(setting);
            }
            else
            {
                // 记录历史
                await CreateHistoryAsync(setting, jsonValue, reason);
                
                // 更新设置
                setting.Value = jsonValue;
                setting.ValueType = SettingValueType.Json; // 确保值类型为JSON
                setting.Version++;
            }
            
            await _context.SaveChangesAsync();
            
            // 更新缓存
            var cacheKey = GenerateCacheKey("Global", module, key);
            await SetCacheAsync(cacheKey, jsonValue);
            
            var objCacheKey = GenerateCacheKey("GlobalObj", module, key, typeof(T).Name);
            await SetCacheAsync(objCacheKey, value);
            
            // 移除相关联的缓存
            await RemoveCacheAsync(GenerateCacheKey("AllGlobal", module));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "序列化并设置全局设置对象时出错: {Module}, {Key}", module, key);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> SetUserSettingAsync(string module, string key, string value, string userId, string? reason = null)
    {
        try
        {
            var setting = await _context.SettingItems
                .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.User && s.ScopeId == userId)
                .FirstOrDefaultAsync();
                
            if (setting == null)
            {
                // 查询全局设置项定义
                var globalSetting = await _context.SettingItems
                    .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
                    .FirstOrDefaultAsync();
                
                // 创建新用户设置
                setting = new SettingItem
                {
                    Module = module,
                    Key = key,
                    Value = value,
                    Name = globalSetting?.Name ?? key, // 使用全局设置的名称，如果不存在则使用键
                    Description = globalSetting?.Description,
                    Scope = SettingScope.User,
                    ScopeId = userId,
                    ValueType = globalSetting?.ValueType ?? SettingValueType.String,
                    Version = 1
                };
                
                _context.SettingItems.Add(setting);
            }
            else
            {
                // 记录历史
                await CreateHistoryAsync(setting, value, reason);
                
                // 更新设置
                setting.Value = value;
                setting.Version++;
            }
            
            await _context.SaveChangesAsync();
            
            // 更新缓存
            var cacheKey = GenerateCacheKey("User", module, key, userId);
            await SetCacheAsync(cacheKey, value);
            
            // 移除相关联的缓存
            await RemoveCacheAsync(GenerateCacheKey("AllUser", module, userId));
            await RemoveCacheAsync(GenerateCacheKey("UserObj", module, key, userId));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置用户设置时出错: {Module}, {Key}, {UserId}", module, key, userId);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> SetUserSettingAsync<T>(string module, string key, T value, string userId, string? reason = null) where T : class
    {
        try
        {
            string jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
            
            var setting = await _context.SettingItems
                .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.User && s.ScopeId == userId)
                .FirstOrDefaultAsync();
                
            if (setting == null)
            {
                // 查询全局设置项定义
                var globalSetting = await _context.SettingItems
                    .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
                    .FirstOrDefaultAsync();
                
                // 创建新用户设置
                setting = new SettingItem
                {
                    Module = module,
                    Key = key,
                    Value = jsonValue,
                    Name = globalSetting?.Name ?? key, // 使用全局设置的名称，如果不存在则使用键
                    Description = globalSetting?.Description,
                    Scope = SettingScope.User,
                    ScopeId = userId,
                    ValueType = SettingValueType.Json, // 设置值类型为JSON
                    Version = 1
                };
                
                _context.SettingItems.Add(setting);
            }
            else
            {
                // 记录历史
                await CreateHistoryAsync(setting, jsonValue, reason);
                
                // 更新设置
                setting.Value = jsonValue;
                setting.ValueType = SettingValueType.Json; // 确保值类型为JSON
                setting.Version++;
            }
            
            await _context.SaveChangesAsync();
            
            // 更新缓存
            var cacheKey = GenerateCacheKey("User", module, key, userId);
            await SetCacheAsync(cacheKey, jsonValue);
            
            var objCacheKey = GenerateCacheKey("UserObj", module, key, userId, typeof(T).Name);
            await SetCacheAsync(objCacheKey, value);
            
            // 移除相关联的缓存
            await RemoveCacheAsync(GenerateCacheKey("AllUser", module, userId));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "序列化并设置用户设置对象时出错: {Module}, {Key}, {UserId}", module, key, userId);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> BatchSetGlobalSettingsAsync(string module, Dictionary<string, string> settings, string? reason = null)
    {
        try
        {
            foreach (var kvp in settings)
            {
                await SetGlobalSettingAsync(module, kvp.Key, kvp.Value, reason);
            }
            
            // 直接更新全局设置缓存
            var cacheKey = GenerateCacheKey("AllGlobal", module);
            await SetCacheAsync(cacheKey, settings);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量设置全局设置时出错: {Module}", module);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> BatchSetUserSettingsAsync(string module, Dictionary<string, string> settings, string userId, string? reason = null)
    {
        try
        {
            // 获取当前所有用户设置（合并全局设置）
            var currentSettings = await GetAllUserSettingsAsync(module, userId);
            
            foreach (var kvp in settings)
            {
                await SetUserSettingAsync(module, kvp.Key, kvp.Value, userId, reason);
                currentSettings[kvp.Key] = kvp.Value;
            }
            
            // 更新合并后的缓存
            var cacheKey = GenerateCacheKey("AllUser", module, userId);
            await SetCacheAsync(cacheKey, currentSettings);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量设置用户设置时出错: {Module}, {UserId}", module, userId);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> ResetUserSettingToDefaultAsync(string module, string? key, string userId)
    {
        try
        {
            if (key == null)
            {
                // 删除所有用户设置
                var userSettings = await _context.SettingItems
                    .Where(s => s.Module == module && s.Scope == SettingScope.User && s.ScopeId == userId)
                    .ToListAsync();
                    
                _context.SettingItems.RemoveRange(userSettings);
                
                // 清除与用户相关的所有缓存
                await RemoveCacheAsync(GenerateCacheKey("AllUser", module, userId));
                foreach (var setting in userSettings)
                {
                    await RemoveCacheAsync(GenerateCacheKey("User", module, setting.Key, userId));
                    await RemoveCacheAsync(GenerateCacheKey("UserObj", module, setting.Key, userId));
                }
            }
            else
            {
                // 删除特定用户设置
                var userSetting = await _context.SettingItems
                    .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.User && s.ScopeId == userId)
                    .FirstOrDefaultAsync();
                    
                if (userSetting != null)
                {
                    _context.SettingItems.Remove(userSetting);
                    
                    // 清除相关缓存
                    await RemoveCacheAsync(GenerateCacheKey("User", module, key, userId));
                    await RemoveCacheAsync(GenerateCacheKey("UserObj", module, key, userId));
                    await RemoveCacheAsync(GenerateCacheKey("AllUser", module, userId));
                }
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户设置时出错: {Module}, {Key}, {UserId}", module, key, userId);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<SettingItem?> GetSettingDefinitionAsync(string module, string key)
    {
        var cacheKey = GenerateCacheKey("Definition", module, key);
        var cachedValue = await GetFromCacheAsync<SettingItem>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var definition = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
            
        if (definition != null)
        {
            await SetCacheAsync(cacheKey, definition);
        }
        
        return definition;
    }
    
    /// <inheritdoc/>
    public async Task<List<SettingItem>> GetAllSettingDefinitionsAsync(string module)
    {
        var cacheKey = GenerateCacheKey("AllDefinitions", module);
        var cachedValue = await GetFromCacheAsync<List<SettingItem>>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var definitions = await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.Global)
            .OrderBy(s => s.Order)
            .ToListAsync();
            
        await SetCacheAsync(cacheKey, definitions);
        
        return definitions;
    }
    
    /// <inheritdoc/>
    public async Task<bool> CreateOrUpdateSettingDefinitionAsync(SettingItem settingItem)
    {
        try
        {
            // 确保作用域为全局
            settingItem.Scope = SettingScope.Global;
            
            var existingSetting = await _context.SettingItems
                .Where(s => s.Module == settingItem.Module && s.Key == settingItem.Key && s.Scope == SettingScope.Global)
                .FirstOrDefaultAsync();
                
            if (existingSetting == null)
            {
                // 创建新定义
                settingItem.Version = 1;
                _context.SettingItems.Add(settingItem);
            }
            else
            {
                // 记录历史
                await CreateHistoryAsync(existingSetting, settingItem.Value, null);
                
                // 更新定义
                existingSetting.Name = settingItem.Name;
                existingSetting.Description = settingItem.Description;
                existingSetting.Value = settingItem.Value;
                existingSetting.ValueType = settingItem.ValueType;
                existingSetting.Options = settingItem.Options;
                existingSetting.Group = settingItem.Group;
                existingSetting.Order = settingItem.Order;
                existingSetting.IsSystemDefault = settingItem.IsSystemDefault;
                existingSetting.Version++;
            }
            
            await _context.SaveChangesAsync();
            
            // 清除相关缓存
            var defCacheKey = GenerateCacheKey("Definition", settingItem.Module, settingItem.Key);
            await RemoveCacheAsync(defCacheKey);
            
            var allDefCacheKey = GenerateCacheKey("AllDefinitions", settingItem.Module);
            await RemoveCacheAsync(allDefCacheKey);
            
            // 清除全局设置相关缓存
            await RemoveCacheAsync(GenerateCacheKey("Global", settingItem.Module, settingItem.Key));
            await RemoveCacheAsync(GenerateCacheKey("GlobalObj", settingItem.Module, settingItem.Key));
            await RemoveCacheAsync(GenerateCacheKey("AllGlobal", settingItem.Module));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建或更新设置定义时出错: {Module}, {Key}", settingItem.Module, settingItem.Key);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> DeleteSettingDefinitionAsync(string module, string key)
    {
        try
        {
            var setting = await _context.SettingItems
                .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
                .FirstOrDefaultAsync();
                
            if (setting != null)
            {
                _context.SettingItems.Remove(setting);
                await _context.SaveChangesAsync();
                
                // 清除相关缓存
                await RemoveCacheAsync(GenerateCacheKey("Definition", module, key));
                await RemoveCacheAsync(GenerateCacheKey("AllDefinitions", module));
                await RemoveCacheAsync(GenerateCacheKey("Global", module, key));
                await RemoveCacheAsync(GenerateCacheKey("GlobalObj", module, key));
                await RemoveCacheAsync(GenerateCacheKey("AllGlobal", module));
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除设置定义时出错: {Module}, {Key}", module, key);
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<List<SettingHistory>> GetSettingHistoryAsync(string module, string key)
    {
        var cacheKey = GenerateCacheKey("History", module, key);
        var cachedValue = await GetFromCacheAsync<List<SettingHistory>>(cacheKey);
        
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        var setting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
            
        if (setting == null)
        {
            return new List<SettingHistory>();
        }
        
        var history = await _context.SettingHistories
            .Where(h => h.SettingId == setting.Id)
            .OrderByDescending(h => h.Version)
            .ToListAsync();
            
        // 使用较短的缓存时间，因为历史记录不经常变化但可能增加
        await SetCacheAsync(cacheKey, history, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
        
        return history;
    }
    
    /// <inheritdoc/>
    public async Task<string> ExportSettingsAsync(string module)
    {
        var settings = await GetAllSettingDefinitionsAsync(module);
            
        return System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
    
    /// <inheritdoc/>
    public async Task<bool> ImportSettingsAsync(string module, string settingsJson)
    {
        try
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize<List<SettingItem>>(settingsJson);
            if (settings == null)
            {
                return false;
            }
            
            foreach (var setting in settings)
            {
                // 确保模块和作用域正确
                setting.Module = module;
                setting.Scope = SettingScope.Global;
                
                await CreateOrUpdateSettingDefinitionAsync(setting);
            }
            
            // 清除与模块相关的所有缓存
            await RemoveModuleCachesAsync(module);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入设置时出错: {Module}", module);
            return false;
        }
    }
    
    /// <summary>
    /// 创建设置历史记录
    /// </summary>
    private async Task CreateHistoryAsync(SettingItem setting, string newValue, string? reason)
    {
        var history = new SettingHistory
        {
            SettingId = setting.Id,
            OldValue = setting.Value,
            NewValue = newValue,
            Version = setting.Version,
            Reason = reason
        };
        
        _context.SettingHistories.Add(history);
        
        // 清除历史记录缓存
        await RemoveCacheAsync(GenerateCacheKey("History", setting.Module, setting.Key));
    }
} 