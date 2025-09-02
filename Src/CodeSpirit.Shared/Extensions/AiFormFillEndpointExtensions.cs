using CodeSpirit.Shared.Middleware;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Shared.Extensions;

/// <summary>
/// AI表单填充端点扩展
/// </summary>
public static class AiFormFillEndpointExtensions
{
    /// <summary>
    /// 使用AI表单填充中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="assembliesToScan">要扫描的程序集，如果为空则扫描所有已加载的程序集</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseAiFormFillEndpoints(this IApplicationBuilder app, params Assembly[] assembliesToScan)
    {
        // 获取端点扫描器并执行扫描
        var scanner = app.ApplicationServices.GetRequiredService<AiFormFillEndpointScanner>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<AiFormFillEndpointScanner>>();

        try
        {
            // 如果没有指定程序集，则扫描所有已加载的程序集
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
    /// 添加AI表单填充端点服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFillEndpoints(this IServiceCollection services)
    {
        // 注册端点扫描器
        services.AddSingleton<AiFormFillEndpointScanner>();
        
        // 确保AI表单填充服务已注册
        services.AddAiFormFill();

        return services;
    }

    /// <summary>
    /// 扫描指定程序集中的AI填充DTO
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembliesToScan">要扫描的程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ScanAiFormFillAssemblies(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        // 注册程序集信息，供后续扫描使用
        services.AddSingleton<AssemblyScanner>(provider => new AssemblyScanner(assembliesToScan));
        
        return services;
    }
}

/// <summary>
/// 程序集扫描器
/// </summary>
public class AssemblyScanner
{
    /// <summary>
    /// 要扫描的程序集
    /// </summary>
    public Assembly[] Assemblies { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="assemblies">程序集数组</param>
    public AssemblyScanner(Assembly[] assemblies)
    {
        Assemblies = assemblies;
    }
}
