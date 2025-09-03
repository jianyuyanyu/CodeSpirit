using CodeSpirit.AiFormFill.Middleware;
using CodeSpirit.AiFormFill.Services;

namespace CodeSpirit.AiFormFill;

/// <summary>
/// AI表单填充服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加AI表单填充服务（基础版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFill(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddScoped<IAiFormFillService, AiFormFillService>();
        services.AddScoped<AiFormPromptBuilder>();
        services.AddScoped<AiFormResponseParser>();
        
        // 添加内存缓存
        services.AddMemoryCache();
        
        return services;
    }
    
    /// <summary>
    /// 添加AI表单填充自动端点服务（推荐）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFillEndpoints(this IServiceCollection services)
    {
        // 检查LLM服务依赖
        ValidateLLMDependency(services);
        
        // 注册端点扫描器
        services.AddSingleton<AiFormFillEndpointScanner>();
        
        // 注册基础服务
        services.AddAiFormFill();
        
        return services;
    }
    
    /// <summary>
    /// 使用AI表单填充中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="assembliesToScan">要扫描的程序集</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseAiFormFillEndpoints(this IApplicationBuilder app, params Assembly[] assembliesToScan)
    {
        // 执行端点扫描
        var scanner = app.ApplicationServices.GetRequiredService<AiFormFillEndpointScanner>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<AiFormFillEndpointScanner>>();

        try
        {
            // 默认扫描CodeSpirit相关程序集
            if (assembliesToScan == null || assembliesToScan.Length == 0)
            {
                assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && 
                               (a.FullName?.Contains("CodeSpirit") == true || 
                                a.FullName?.Contains("ExamApi") == true ||
                                a.FullName?.Contains("SurveyApi") == true))
                    .ToArray();
            }

            scanner.ScanAssemblies(assembliesToScan);
            var endpoints = scanner.GetEndpoints();
            logger.LogInformation("AI表单填充自动端点扫描完成，发现 {Count} 个端点", endpoints.Count);

            foreach (var endpoint in endpoints.Values)
            {
                logger.LogDebug("注册AI填充端点: {Route} -> {DtoType}", 
                    endpoint.Route, endpoint.DtoType.Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI表单填充端点扫描失败");
        }

        // 注册中间件
        app.UseMiddleware<AiFormFillMiddleware>();
        return app;
    }
    
    /// <summary>
    /// 验证LLM服务依赖
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void ValidateLLMDependency(IServiceCollection services)
    {
        var hasLLMServices = services.Any(x => x.ServiceType.FullName?.Contains("CodeSpirit.LLM") == true);
        
        if (!hasLLMServices)
        {
            throw new InvalidOperationException(
                "AI表单填充服务需要LLM服务支持，请先调用 AddLLMServices() 注册LLM服务");
        }
    }
}
