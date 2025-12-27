using CodeSpirit.Core;
using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Localization.Constants;
using CodeSpirit.Localization.Models;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Localization.Providers;

/// <summary>
/// 用户设置语言提供者
/// </summary>
public class UserSettingsLanguageProvider : ILanguageProvider, IScopedDependency
{
    private readonly ISettingsService _settingsService;
    private readonly ICurrentUser _currentUser;
    private readonly LocalizationOptions _options;

    public UserSettingsLanguageProvider(
        ISettingsService settingsService,
        ICurrentUser currentUser,
        IOptions<LocalizationOptions> options)
    {
        _settingsService = settingsService;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<string?> GetLanguageAsync()
    {
        if (!_options.EnableUserLevelLanguage)
        {
            return null;
        }

        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
        {
            return null;
        }

        var language = await _settingsService.GetUserSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.UserPreference,
            _currentUser.Id.Value.ToString()
        );

        return language;
    }
}
