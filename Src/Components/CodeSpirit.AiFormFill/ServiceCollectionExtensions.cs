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
    /// <remarks>
    /// 此方法注册AI表单填充的核心服务。
    /// 
    /// <para>
    /// <strong>LLM审计支持（可选）：</strong>
    /// 如果在调用此方法前已通过 AddLLMAuditServices() 注册了LLM审计服务，
    /// AI表单填充组件将自动记录所有LLM交互的审计日志，包括：
    /// </para>
    /// <list type="bullet">
    ///   <item><description>提示词和响应内容</description></item>
    ///   <item><description>处理时间和Token使用量</description></item>
    ///   <item><description>成功/失败状态</description></item>
    ///   <item><description>业务元数据（DTO类型、触发字段等）</description></item>
    /// </list>
    /// <para>
    /// 如果未注册审计服务，AI表单填充功能仍可正常工作，只是不会记录审计日志。
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAiFormFill(this IServiceCollection services)
    {
        // 注册核心服务
        services.AddScoped<IAiFormFillService, AiFormFillService>();
        services.AddScoped<AiFormPromptBuilder>();
        services.AddScoped<AiFormResponseParser>();
        
        // 注册AI表单填充专用LLM服务
        services.AddScoped<AiFormFillLLMClientFactory>();
        
        // 注册HTTP客户端（用于AI表单填充专用LLM）
        services.AddHttpClient("AiFormFillLLMClient");
        
        // 添加内存缓存
        services.AddMemoryCache();
        
        // 注意：LLM审计服务（ILLMAuditService）、租户上下文（ITenantContext）
        // 和HTTP上下文访问器（IHttpContextAccessor）作为可选依赖，
        // 如果已注册，将自动启用审计功能；如果未注册，不影响表单填充功能。
        
        return services;
    }
    
    /// <summary>
    /// 添加AI表单填充自动端点服务（推荐）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 此方法在 AddAiFormFill() 的基础上增加了自动端点扫描功能。
    /// 
    /// <para>
    /// <strong>依赖要求：</strong>
    /// </para>
    /// <list type="number">
    ///   <item><description>必需：LLM服务（通过 AddLLMServices() 注册）</description></item>
    ///   <item><description>可选：LLM审计服务（通过 AddLLMAuditServices() 注册）</description></item>
    /// </list>
    /// <para>
    /// <strong>推荐的服务注册顺序：</strong>
    /// </para>
    /// <code>
    /// // 1. 注册LLM服务（必需）
    /// services.AddLLMServices();
    /// 
    /// // 2. 注册LLM审计服务（可选，用于成本统计和合规审计）
    /// services.AddLLMAuditServices(configuration);
    /// 
    /// // 3. 注册AI表单填充服务
    /// services.AddAiFormFillEndpoints();
    /// </code>
    /// </remarks>
    public static IServiceCollection AddAiFormFillEndpoints(this IServiceCollection services)
    {
        // 检查LLM服务依赖
        ValidateLLMDependency(services);
        
        // 注册端点扫描器
        services.AddSingleton<AiFormFillEndpointScanner>();
        
        // 注册基础服务（包含审计支持）
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
    /// 添加AI表单填充独立LLM配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAiFormFillIndependentLLM(
        this IServiceCollection services,
        Action<AiFormFillLLMOptions> configureOptions)
    {
        var options = new AiFormFillLLMOptions();
        configureOptions(options);
        
        services.AddSingleton(options);
        
        return services;
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

/// <summary>
/// AI表单填充LLM配置选项
/// </summary>
public class AiFormFillLLMOptions
{
    /// <summary>
    /// 默认设置键名
    /// </summary>
    public string DefaultSettingsKey { get; set; } = "AiFormFillLLM";
    
    /// <summary>
    /// 是否启用独立LLM配置
    /// </summary>
    public bool EnableIndependentLLM { get; set; } = true;
    
    /// <summary>
    /// 默认是否禁用思考
    /// </summary>
    public bool DefaultDisableThinking { get; set; } = true;
    
    /// <summary>
    /// 默认响应格式类型
    /// </summary>
    public string DefaultResponseFormatType { get; set; } = "json_object";
    
    /// <summary>
    /// 默认温度参数
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.1;
    
    /// <summary>
    /// 默认Top-p参数
    /// </summary>
    public double DefaultTopP { get; set; } = 0.9;
}
