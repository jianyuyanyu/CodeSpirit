using CodeSpirit.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeSpirit.Shared.Extensions;

/// <summary>
/// AI表单填充服务注册扩展
/// </summary>
public static class AiFormFillServiceCollectionExtensions
{
    /// <summary>
    /// 添加AI表单填充服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFill(this IServiceCollection services)
    {
        // 注册AI表单填充相关服务
        services.AddScoped<IAiFormFillService, AiFormFillService>();
        services.AddScoped<AiFormPromptBuilder>();
        services.AddScoped<AiFormResponseParser>();
        
        // 添加内存缓存（如果还没有注册）
        services.AddMemoryCache();
        
        return services;
    }
    
    /// <summary>
    /// 添加AI表单填充服务（带LLM依赖检查）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFillWithLLMCheck(this IServiceCollection services)
    {
        // 检查是否已经注册了LLM服务
        var hasLLMServices = services.Any(x => x.ServiceType == typeof(CodeSpirit.LLM.Settings.ISettingsProvider));
        
        if (!hasLLMServices)
        {
            throw new InvalidOperationException(
                "AI表单填充服务需要LLM服务支持，请先调用 AddLLMServices<TSettingsProvider>() 注册LLM服务");
        }
        
        return services.AddAiFormFill();
    }
}
