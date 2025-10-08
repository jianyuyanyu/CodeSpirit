using CodeSpirit.ExamApi.Services.Helpers;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.LLM;
using CodeSpirit.Shared.Notifications;

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
        // 添加LLM服务（使用统一配置）
        services.AddLLMServices();
        
        // 注册工具类
        services.AddSingleton<IPromptBuilder, DefaultPromptBuilder>();
        services.AddSingleton<IQuestionParser, DefaultQuestionParser>();
        
        // 注册主服务
        services.AddScoped<IAIQuestionGeneratorService, AIQuestionGeneratorService>();
        services.AddScoped<QuestionAiGeneratorService>();
        
        // 注册通知服务
        services.AddScoped<ISessionNotificationService, SessionNotificationService>();
        services.AddScoped<IGeneratorNotificationService, GeneratorNotificationService>();
        
        // 注册题目验证和修复服务
        services.AddScoped<IQuestionValidationService, QuestionValidationService>();
        
        return services;
    }
} 