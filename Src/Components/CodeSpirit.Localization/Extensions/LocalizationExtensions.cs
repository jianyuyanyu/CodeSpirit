using CodeSpirit.Localization.Models;
using CodeSpirit.Localization.Providers;
using CodeSpirit.Localization.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace CodeSpirit.Localization.Extensions;

/// <summary>
/// 本地化扩展方法
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// 添加 CodeSpirit 本地化服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritLocalization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 配置选项
        var localizationSection = configuration.GetSection("Localization");
        services.Configure<Models.LocalizationOptions>(localizationSection);

        var options = localizationSection.Get<Models.LocalizationOptions>() ?? new Models.LocalizationOptions();

        // 注册各个语言提供者（不直接注册为 ILanguageProvider）
        services.AddScoped<CookieLanguageProvider>();
        if (options.EnableUserLevelLanguage)
        {
            services.AddScoped<UserSettingsLanguageProvider>();
        }
        if (options.EnableTenantLevelLanguage)
        {
            services.AddScoped<TenantSettingsLanguageProvider>();
        }
        services.AddScoped<GlobalSettingsLanguageProvider>();

        // 注册组合语言提供者（作为主要的 ILanguageProvider）
        services.AddScoped<ILanguageProvider>(provider =>
        {
            var orderedProviders = new List<ILanguageProvider>
            {
                provider.GetRequiredService<CookieLanguageProvider>()
            };
            
            if (options.EnableUserLevelLanguage)
            {
                orderedProviders.Add(provider.GetRequiredService<UserSettingsLanguageProvider>());
            }
            
            if (options.EnableTenantLevelLanguage)
            {
                orderedProviders.Add(provider.GetRequiredService<TenantSettingsLanguageProvider>());
            }
            
            orderedProviders.Add(provider.GetRequiredService<GlobalSettingsLanguageProvider>());
            
            return new CompositeLanguageProvider(orderedProviders);
        });

        // 注册本地化服务
        services.AddScoped<ILocalizationService, LocalizationService>();
        
        // 注册语言配置初始化服务
        services.AddSingleton<LocalizationSettingsInitializer>();

        // 添加 ASP.NET Core 本地化服务
        var supportedCultures = options.SupportedCultures
            .Select(c => new CultureInfo(c.Code))
            .ToArray();

        services.Configure<RequestLocalizationOptions>(opts =>
        {
            opts.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
            opts.SupportedCultures = supportedCultures;
            opts.SupportedUICultures = supportedCultures;
            opts.FallBackToParentCultures = options.FallbackToParentCultures;
            opts.FallBackToParentUICultures = options.FallbackToParentCultures;
        });

        services.AddLocalization();

        return services;
    }

    /// <summary>
    /// 配置 DataAnnotations 验证特性的本地化支持
    /// </summary>
    /// <param name="builder">MVC 构建器</param>
    /// <returns>IMvcBuilder</returns>
    public static IMvcBuilder AddCodeSpiritDataAnnotationsLocalization(this IMvcBuilder builder)
    {
        return builder.AddDataAnnotationsLocalization(options =>
        {
            options.DataAnnotationLocalizerProvider = (type, factory) =>
            {
                // 使用 ValidationResources 作为默认的验证消息本地化器
                var validationType = Type.GetType("CodeSpirit.Localization.Resources.ValidationResources, CodeSpirit.Localization");
                if (validationType != null)
                {
                    return factory.Create(validationType);
                }
                // 回退到类型本身
                return factory.Create(type);
            };
        });
    }

    /// <summary>
    /// 使用 CodeSpirit 请求本地化中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseCodeSpiritRequestLocalization(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // 从当前请求的作用域获取本地化服务
            var localizationService = context.RequestServices.GetRequiredService<ILocalizationService>();
            
            var language = await localizationService.GetCurrentLanguageAsync();
            var culture = new CultureInfo(language);
            
            // 设置线程文化信息
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            
            // 设置 IRequestCultureFeature，供其他组件使用
            // 这样 CachingHelper 和 UtilityHelper 可以从此特性获取语言
            var requestCulture = new RequestCulture(culture);
            var requestCultureFeature = new RequestCultureFeature(requestCulture, null);
            context.Features.Set<IRequestCultureFeature>(requestCultureFeature);

            await next();
        });

        // 注意：由于自定义中间件已经设置了 CultureInfo 和 IRequestCultureFeature，
        // 这里的 UseRequestLocalization() 主要是为了向后兼容和设置其他默认行为
        // 如果未来确认无需此调用，可以移除
        app.UseRequestLocalization();

        return app;
    }
}
