using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Localization.Constants;
using CodeSpirit.Localization.Models;
using CodeSpirit.Localization.Providers;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Localization.Services;

/// <summary>
/// 本地化服务实现
/// </summary>
public class LocalizationService : ILocalizationService, IScopedDependency
{
    private readonly ILanguageProvider _languageProvider;
    private readonly ISettingsService _settingsService;
    private readonly LocalizationOptions _options;

    public LocalizationService(
        ILanguageProvider languageProvider,
        ISettingsService settingsService,
        IOptions<LocalizationOptions> options)
    {
        _languageProvider = languageProvider;
        _settingsService = settingsService;
        _options = options.Value;
    }

    public async Task<string> GetCurrentLanguageAsync()
    {
        var language = await _languageProvider.GetLanguageAsync();
        return language ?? _options.DefaultCulture;
    }

    public async Task<bool> SetUserLanguageAsync(string userId, string language)
    {
        if (!_options.EnableUserLevelLanguage)
        {
            return false;
        }

        return await _settingsService.SetUserSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.UserPreference,
            language,
            userId,
            "用户设置语言偏好"
        );
    }

    public async Task<bool> SetTenantLanguageAsync(string tenantId, string language)
    {
        if (!_options.EnableTenantLevelLanguage)
        {
            return false;
        }

        return await _settingsService.SetTenantSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.TenantDefault,
            language,
            tenantId,
            "租户设置默认语言"
        );
    }

    public async Task<bool> SetGlobalLanguageAsync(string language)
    {
        return await _settingsService.SetGlobalSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.GlobalDefault,
            language,
            "系统设置全局默认语言"
        );
    }
}
