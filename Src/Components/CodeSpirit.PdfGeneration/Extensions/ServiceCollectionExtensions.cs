using CodeSpirit.PdfGeneration.Options;
using CodeSpirit.PdfGeneration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.PdfGeneration.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加PDF生成服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPdfGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置选项
        services.Configure<PdfGenerationOptions>(configuration.GetSection("PdfGeneration"));
        
        // 注册PDF生成服务
        services.TryAddSingleton<IPdfGenerationService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PdfGenerationOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<PdfGenerationService>>();
            var browserPoolLogger = sp.GetRequiredService<ILogger<BrowserPool>>();
            return new PdfGenerationService(options, logger, browserPoolLogger);
        });
        
        return services;
    }
    
    /// <summary>
    /// 添加PDF生成服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPdfGeneration(this IServiceCollection services, Action<PdfGenerationOptions> configureOptions)
    {
        // 注册配置选项
        services.Configure(configureOptions);
        
        // 注册PDF生成服务
        services.TryAddSingleton<IPdfGenerationService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PdfGenerationOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<PdfGenerationService>>();
            var browserPoolLogger = sp.GetRequiredService<ILogger<BrowserPool>>();
            return new PdfGenerationService(options, logger, browserPoolLogger);
        });
        
        return services;
    }
    
    /// <summary>
    /// 添加PDF生成服务并初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static async Task<IApplicationBuilder> UsePdfGenerationAsync(this IApplicationBuilder app)
    {
        // 获取PDF生成服务并初始化
        var logger = app.ApplicationServices.GetRequiredService<ILogger<PdfGenerationService>>();
        logger.LogInformation("正在初始化PDF生成服务...");
        
        var pdfService = app.ApplicationServices.GetRequiredService<IPdfGenerationService>();
        await pdfService.InitializeAsync();
        
        logger.LogInformation("PDF生成服务初始化完成");
        
        return app;
    }
}