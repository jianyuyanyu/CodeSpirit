using CodeSpirit.LLM.Clients;
using CodeSpirit.LLM.Factories;
using CodeSpirit.LLM.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.LLM;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加LLM服务（使用基于配置的设置提供者）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 这是推荐的使用方式，会自动从IConfiguration中读取LLM设置
    /// </remarks>
    public static IServiceCollection AddLLMServices(this IServiceCollection services)
    {
        // 注册默认的基于配置的设置提供者
        services.AddScoped<ISettingsProvider, ConfigurationBasedSettingsProvider>();
        
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
    /// <remarks>
    /// 当需要使用自定义设置提供者（如从数据库读取设置）时使用此方法
    /// </remarks>
    public static IServiceCollection AddLLMServices<TSettingsProvider>(this IServiceCollection services)
        where TSettingsProvider : class, ISettingsProvider
    {
        // 注册设置提供者
        services.AddScoped<ISettingsProvider, TSettingsProvider>();
        
        // 注册HTTP客户端
        services.AddHttpClient("LLMClient");
        
        // 注册工厂和客户端
        services.AddScoped<ILLMClientFactory, DefaultLLMClientFactory>();
        
        // 注册LLM助手
        services.AddScoped<LLMAssistant>();
        
        return services;
    }
}
