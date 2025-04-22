using CodeSpirit.ExamApi.Services.Helpers;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.Settings;
using CodeSpirit.LLM;

namespace CodeSpirit.ExamApi.Extensions;

/// <summary>
/// 依赖注入扩展类
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 添加AI题目生成相关服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAIQuestionGeneratorServices(this IServiceCollection services)
    {
        // 添加LLM服务，使用自定义设置提供者
        services.AddLLMServices<ExamLLMSettingsProvider>();
        
        // 注册工具类
        services.AddSingleton<IPromptBuilder, DefaultPromptBuilder>();
        services.AddSingleton<IQuestionParser, DefaultQuestionParser>();
        
        // 注册主服务
        services.AddScoped<IAIQuestionGeneratorService, AIQuestionGeneratorService>();
        
        return services;
    }
} 