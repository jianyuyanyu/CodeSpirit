using CodeSpirit.Core.DependencyInjection;
using CodeSpirit.Localization.Constants;
using CodeSpirit.Settings.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CodeSpirit.Localization.Services;

/// <summary>
/// 语言配置初始化服务
/// </summary>
public class LocalizationSettingsInitializer : ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public LocalizationSettingsInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 初始化语言配置
    /// </summary>
    public async Task InitializeAsync()
    {
        // 创建一个新的作用域来解析 Scoped 服务
        using var scope = _serviceProvider.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        
        // 初始化全局默认语言
        var globalLang = await settingsService.GetGlobalSettingAsync(
            CultureConstants.SettingsModule,
            CultureConstants.GlobalDefaultLanguageKey);

        if (string.IsNullOrEmpty(globalLang))
        {
            await settingsService.SetGlobalSettingAsync(
                CultureConstants.SettingsModule,
                CultureConstants.GlobalDefaultLanguageKey,
                CultureConstants.DefaultCulture,
                "系统初始化默认语言"
            );
        }

        // 初始化支持的语言列表
        var supportedLangs = await settingsService.GetGlobalSettingAsync(
            CultureConstants.SettingsModule,
            "SupportedLanguages");

        if (string.IsNullOrEmpty(supportedLangs))
        {
            var languages = new[]
            {
                new { Code = CultureConstants.Chinese, Name = "简体中文" },
                new { Code = CultureConstants.English, Name = "English" }
            };

            await settingsService.SetGlobalSettingAsync(
                CultureConstants.SettingsModule,
                "SupportedLanguages",
                JsonSerializer.Serialize(languages),
                "系统初始化支持的语言列表"
            );
        }
    }
}
