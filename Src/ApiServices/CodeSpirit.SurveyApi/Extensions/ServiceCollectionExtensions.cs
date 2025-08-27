using CodeSpirit.LLM;
using CodeSpirit.SurveyApi.Services.Implementations;

namespace CodeSpirit.SurveyApi.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加问卷LLM服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSurveyLLMServices(this IServiceCollection services)
    {
        // 使用LLMSettingsProvider作为ISettingsProvider的实现
        services.AddLLMServices<LLMSettingsProvider>();
        
        return services;
    }
}
