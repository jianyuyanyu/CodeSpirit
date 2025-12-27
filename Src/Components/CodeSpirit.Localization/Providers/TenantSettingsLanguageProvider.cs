using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Localization.Models;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CodeSpirit.Localization.Providers;

/// <summary>
/// 租户设置语言提供者
/// </summary>
public class TenantSettingsLanguageProvider : ILanguageProvider, IScopedDependency
{
    private readonly ISettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly LocalizationOptions _options;

    public TenantSettingsLanguageProvider(
        ISettingsService settingsService,
        ITenantContext tenantContext,
        IOptions<LocalizationOptions> options)
    {
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public async Task<string?> GetLanguageAsync()
    {
        if (!_options.EnableTenantLevelLanguage)
        {
            return null;
        }

        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrEmpty(tenantId))
        {
            return null;
        }

        var language = await _settingsService.GetTenantSettingAsync(
            _options.SettingsModule,
            _options.SettingsKeys.TenantDefault,
            tenantId
        );

        return language;
    }
}
