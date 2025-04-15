using Microsoft.EntityFrameworkCore;
using CodeSpirit.Settings.Data;
using System.Text.Json;

namespace CodeSpirit.Settings.Services.Implementations;

/// <summary>
/// 设置管理服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly SettingsDbContext _context;
    private readonly ILogger<SettingsService> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">设置数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    public SettingsService(SettingsDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<string?> GetGlobalSettingAsync(string module, string key)
    {
        var setting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
            
        return setting?.Value;
    }
    
    /// <inheritdoc/>
    public async Task<T?> GetGlobalSettingAsync<T>(string module, string key) where T : class, new()
    {
        var settingValue = await GetGlobalSettingAsync(module, key);
        
        if (string.IsNullOrEmpty(settingValue))
        {
            return null;
        }
        
        try
        {
            return JsonSerializer.Deserialize<T>(settingValue);
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
        var settings = await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.Global)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
            
        return settings;
    }
    
    /// <inheritdoc/>
    public async Task<string?> GetUserSettingAsync(string module, string key, string userId)
    {
        // 先查询用户特定设置
        var userSetting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.User && s.ScopeId == userId)
            .FirstOrDefaultAsync();
            
        if (userSetting != null)
        {
            return userSetting.Value;
        }
        
        // 如果用户没有特定设置，返回全局设置
        return await GetGlobalSettingAsync(module, key);
    }
    
    /// <inheritdoc/>
    public async Task<T?> GetUserSettingAsync<T>(string module, string key, string userId) where T : class, new()
    {
        var settingValue = await GetUserSettingAsync(module, key, userId);
        
        if (string.IsNullOrEmpty(settingValue))
        {
            return new T();
        }
        
        try
        {
            return JsonSerializer.Deserialize<T>(settingValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化用户设置值时出错: {Module}, {Key}, {UserId}", module, key, userId);
            return new T();
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> GetAllUserSettingsAsync(string module, string userId)
    {
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
            string jsonValue = JsonSerializer.Serialize(value);
            
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
            string jsonValue = JsonSerializer.Serialize(value);
            
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
            foreach (var kvp in settings)
            {
                await SetUserSettingAsync(module, kvp.Key, kvp.Value, userId, reason);
            }
            
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
        return await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
    }
    
    /// <inheritdoc/>
    public async Task<List<SettingItem>> GetAllSettingDefinitionsAsync(string module)
    {
        return await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.Global)
            .OrderBy(s => s.Order)
            .ToListAsync();
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
        var setting = await _context.SettingItems
            .Where(s => s.Module == module && s.Key == key && s.Scope == SettingScope.Global)
            .FirstOrDefaultAsync();
            
        if (setting == null)
        {
            return new List<SettingHistory>();
        }
        
        return await _context.SettingHistories
            .Where(h => h.SettingId == setting.Id)
            .OrderByDescending(h => h.Version)
            .ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<string> ExportSettingsAsync(string module)
    {
        var settings = await _context.SettingItems
            .Where(s => s.Module == module && s.Scope == SettingScope.Global)
            .OrderBy(s => s.Order)
            .ToListAsync();
            
        return JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
    
    /// <inheritdoc/>
    public async Task<bool> ImportSettingsAsync(string module, string settingsJson)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<List<SettingItem>>(settingsJson);
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
    }
} 