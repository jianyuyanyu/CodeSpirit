using CodeSpirit.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

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
}
