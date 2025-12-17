using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Localization.Constants;
using CodeSpirit.Localization.Models;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Localization.Providers;

/// <summary>
/// 全局设置语言提供者（最终回退）
/// </summary>
public class GlobalSettingsLanguageProvider : ILanguageProvider, IScopedDependency
{
    private readonly ISettingsService _settingsService;
    private readonly LocalizationOptions _options;

    public GlobalSettingsLanguageProvider(
        ISettingsService settingsService,
        IOptions<LocalizationOptions> options)
    {
        _settingsService = settingsService;
        _options = options.Value;
    }

    public async Task<string?> GetLanguageAsync()
    {
        var language = await _settingsService.GetGlobalSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.GlobalDefault
        );

        return language ?? _options.DefaultCulture;
    }
}
