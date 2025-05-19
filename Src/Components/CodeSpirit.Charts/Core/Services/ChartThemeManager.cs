using CodeSpirit.Charts.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Charts.Core.Services;

/// <summary>
/// 图表主题管理器的默认实现
/// </summary>
public class ChartThemeManager : IChartThemeManager
{
    private readonly ILogger<ChartThemeManager> _logger;
    private readonly Dictionary<string, Dictionary<string, object>> _themes;

    public ChartThemeManager(ILogger<ChartThemeManager> logger)
    {
        _logger = logger;
        _themes = new Dictionary<string, Dictionary<string, object>>();
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetAvailableThemesAsync(string providerName)
    {
        try
        {
            if (!_themes.TryGetValue(providerName, out var providerThemes))
            {
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            }

            return Task.FromResult<IEnumerable<string>>(providerThemes.Keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available themes for provider '{ProviderName}'", providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<object> GetThemeConfigAsync(string themeName, string providerName)
    {
        try
        {
            if (!_themes.TryGetValue(providerName, out var providerThemes))
            {
                throw new KeyNotFoundException($"No themes found for provider '{providerName}'.");
            }

            if (!providerThemes.TryGetValue(themeName, out var themeConfig))
            {
                throw new KeyNotFoundException($"Theme '{themeName}' not found for provider '{providerName}'.");
            }

            return Task.FromResult(themeConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theme config for theme '{ThemeName}' and provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> RegisterThemeAsync(string themeName, object themeConfig, string providerName)
    {
        try
        {
            if (!_themes.TryGetValue(providerName, out var providerThemes))
            {
                providerThemes = new Dictionary<string, object>();
                _themes[providerName] = providerThemes;
            }

            if (providerThemes.ContainsKey(themeName))
            {
                return Task.FromResult(false);
            }

            providerThemes[themeName] = themeConfig;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering theme '{ThemeName}' for provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateThemeAsync(string themeName, object themeConfig, string providerName)
    {
        try
        {
            if (!_themes.TryGetValue(providerName, out var providerThemes))
            {
                return Task.FromResult(false);
            }

            if (!providerThemes.ContainsKey(themeName))
            {
                return Task.FromResult(false);
            }

            providerThemes[themeName] = themeConfig;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating theme '{ThemeName}' for provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteThemeAsync(string themeName, string providerName)
    {
        try
        {
            if (!_themes.TryGetValue(providerName, out var providerThemes))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(providerThemes.Remove(themeName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting theme '{ThemeName}' for provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object> ApplyThemeAsync(object chartConfig, string themeName, string providerName)
    {
        try
        {
            var themeConfig = await GetThemeConfigAsync(themeName, providerName);
            
            // 这里需要根据不同的图表提供者实现具体的主题应用逻辑
            // 以下是一个简单的示例，实际实现可能需要更复杂的逻辑
            if (chartConfig is Dictionary<string, object> configDict && themeConfig is Dictionary<string, object> themeDict)
            {
                // 深度合并主题配置到图表配置
                MergeThemeConfig(configDict, themeDict);
                return configDict;
            }
            
            // 如果不是字典类型，可能需要其他方式处理
            _logger.LogWarning("Unable to apply theme to chart config of type {ConfigType}", chartConfig.GetType().Name);
            return chartConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying theme '{ThemeName}' to chart config for provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<object> GetThemePreviewAsync(string themeName, string providerName)
    {
        try
        {
            var themeConfig = await GetThemeConfigAsync(themeName, providerName);
            
            // 创建一个简单的预览图表配置
            var previewConfig = CreatePreviewConfig(providerName);
            
            // 应用主题
            return await ApplyThemeAsync(previewConfig, themeName, providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theme preview for theme '{ThemeName}' and provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportThemeAsync(string themeName, string providerName)
    {
        try
        {
            var themeConfig = await GetThemeConfigAsync(themeName, providerName);
            return System.Text.Json.JsonSerializer.Serialize(themeConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting theme '{ThemeName}' for provider '{ProviderName}'", 
                themeName, providerName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> ImportThemeAsync(string themeData, string providerName)
    {
        try
        {
            var themeConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(themeData);
            if (themeConfig == null)
            {
                throw new ArgumentException("Invalid theme data format.");
            }

            if (!themeConfig.TryGetValue("name", out var themeNameObj) || themeNameObj is not string themeName)
            {
                throw new ArgumentException("Theme data must contain a 'name' property.");
            }

            return RegisterThemeAsync(themeName, themeConfig, providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing theme for provider '{ProviderName}'", providerName);
            throw;
        }
    }

    #region Private Methods

    private void MergeThemeConfig(Dictionary<string, object> target, Dictionary<string, object> source)
    {
        foreach (var kvp in source)
        {
            if (kvp.Value is Dictionary<string, object> sourceDict && 
                target.TryGetValue(kvp.Key, out var targetValue) && 
                targetValue is Dictionary<string, object> targetDict)
            {
                // 递归合并嵌套字典
                MergeThemeConfig(targetDict, sourceDict);
            }
            else
            {
                // 直接替换或添加值
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    private object CreatePreviewConfig(string providerName)
    {
        // 创建一个简单的预览图表配置
        // 这里只是一个示例，实际实现可能需要根据不同的图表提供者创建不同的预览配置
        return new Dictionary<string, object>
        {
            ["title"] = new Dictionary<string, object>
            {
                ["text"] = "Theme Preview"
            },
            ["xAxis"] = new Dictionary<string, object>
            {
                ["data"] = new[] { "Category 1", "Category 2", "Category 3", "Category 4", "Category 5" }
            },
            ["yAxis"] = new Dictionary<string, object>(),
            ["series"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["name"] = "Series 1",
                    ["type"] = "bar",
                    ["data"] = new[] { 10, 20, 30, 40, 50 }
                },
                new Dictionary<string, object>
                {
                    ["name"] = "Series 2",
                    ["type"] = "line",
                    ["data"] = new[] { 50, 40, 30, 20, 10 }
                }
            }
        };
    }

    #endregion
}