using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.DependencyInjection;
using CodeSpirit.Shared.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace CodeSpirit.Shared.Startup;

/// <summary>
/// 统一API启动扩展方法
/// </summary>
public static class ApiStartupExtensions
{
    /// <summary>
    /// 添加CodeSpirit API服务
    /// </summary>
    /// <typeparam name="TConfig">API配置类型</typeparam>
    /// <param name="builder">Web应用构建器</param>
    /// <param name="configuration">API配置实例</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritApi<TConfig>(
        this WebApplicationBuilder builder,
        TConfig? configuration = null) 
        where TConfig : class, IApiServiceConfiguration, new()
    {
        var config = configuration ?? new TConfig();
        
        // 基础服务注册
        builder.AddServiceDefaults(config.ServiceName);
        // 添加系统服务 - 使用调用程序集的类型
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        var programType = entryAssembly?.GetType("Program") ?? typeof(ApiStartupExtensions);
        // 使用Scrutor自动注册标记接口的服务 - 使用入口程序集而不是执行程序集
        if (entryAssembly != null)
        {
            builder.Services.AddDependencyInjectionWithScrutor(entryAssembly);
        }
        builder.Services.AddSystemServices(builder.Configuration, programType, builder.Environment);
        
        // 通用API服务
        builder.Services.AddCommonApiServices(builder.Configuration, config.ConnectionStringKey);
        
        // 特定服务配置
        config.ConfigureServices(builder.Services, builder.Configuration);
        
        return builder.Services;
    }
    
    /// <summary>
    /// 配置CodeSpirit API应用
    /// </summary>
    /// <typeparam name="TConfig">API配置类型</typeparam>
    /// <param name="app">Web应用</param>
    /// <param name="configuration">API配置实例</param>
    /// <returns>配置后的Web应用</returns>
    public static async Task<WebApplication> UseCodeSpiritApiAsync<TConfig>(
        this WebApplication app,
        TConfig? configuration = null)
        where TConfig : class, IApiServiceConfiguration, new()
    {
        var config = configuration ?? new TConfig();
        
        // 通用中间件配置（包含插入点）
        await app.UseCommonApiMiddlewareAsync(config);
        
        // 特定中间件配置（在通用中间件之后）
        await config.ConfigureMiddlewareAsync(app);
        
        // 数据库初始化
        await config.InitializeDatabaseAsync(app);
        
        return app;
    }
}
