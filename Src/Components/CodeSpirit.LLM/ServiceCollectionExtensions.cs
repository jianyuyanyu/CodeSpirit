using CodeSpirit.LLM.Clients;
using CodeSpirit.LLM.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.LLM;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加LLM服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services)
    {
        // 注册HTTP客户端
        services.AddHttpClient("LLMClient");
        
        // 注册工厂和客户端
        services.AddScoped<ILLMClientFactory, DefaultLLMClientFactory>();
        
        // 注册LLM助手
        services.AddScoped<LLMAssistant>();
        
        return services;
    }
    
    /// <summary>
    /// 添加LLM服务，并配置设置提供者的实现
    /// </summary>
    /// <typeparam name="TSettingsProvider">设置提供者实现类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLLMServices<TSettingsProvider>(this IServiceCollection services)
        where TSettingsProvider : class, Settings.ISettingsProvider
    {
        // 注册设置提供者
        services.AddScoped<Settings.ISettingsProvider, TSettingsProvider>();
        
        // 注册HTTP客户端
        services.AddHttpClient("LLMClient");
        
        // 注册工厂和客户端
        services.AddScoped<ILLMClientFactory, DefaultLLMClientFactory>();
        
        // 注册LLM助手
        services.AddScoped<LLMAssistant>();
        
        return services;
    }
}
