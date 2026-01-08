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
        
        // 添加配置中心 SDK（在其他服务之前，确保配置在应用启动前加载）
        TryAddConfigCenterSdk(builder);
        
        // 添加系统服务 - 使用调用程序集的类型
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        var programType = entryAssembly?.GetType("Program") ?? typeof(ApiStartupExtensions);
        // 使用Scrutor自动注册标记接口的服务 - 包含入口程序集和共享程序集
        if (entryAssembly != null)
        {
            var assembliesToScan = new[]
            {
                entryAssembly,
                typeof(CodeSpirit.Shared.Services.IAiTaskService).Assembly // CodeSpirit.Shared 程序集
            };
            builder.Services.AddDependencyInjectionWithScrutor(assembliesToScan);
        }
        builder.Services.AddSystemServices(builder.Configuration, programType, builder.Environment);
        
        // 通用API服务
        builder.Services.AddCommonApiServices(builder.Configuration, config.ConnectionStringKey, config.ServiceName, config.PathPrefixOptions);
        
        // 特定服务配置
        config.ConfigureServices(builder.Services, builder.Configuration);
        
        return builder.Services;
    }
    
    /// <summary>
    /// 尝试添加配置中心 SDK（通过反射，避免循环依赖）
    /// </summary>
    private static void TryAddConfigCenterSdk(WebApplicationBuilder builder)
    {
        try
        {
            // 配置中心服务本身不需要配置中心 SDK
            var serviceName = builder.Configuration["ServiceName"] 
                ?? builder.Environment.ApplicationName
                ?? string.Empty;
            
            if (serviceName.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ConfigCenter SDK] 跳过配置中心服务自身: {serviceName}");
                return;
            }
            
            // 通过反射加载配置中心 SDK
            var sdkAssembly = Assembly.Load("CodeSpirit.ConfigCenter.Sdk");
            var extensionsType = sdkAssembly.GetType("CodeSpirit.ConfigCenter.Sdk.Extensions.ConfigCenterExtensions");
            
            if (extensionsType != null)
            {
                // 查找 AddCodeSpiritConfigCenter 方法（扩展方法的第一个参数是 this WebApplicationBuilder）
                var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "AddCodeSpiritConfigCenter")
                    .ToArray();
                
                // 优先使用只有一个参数的重载（builder），如果没有则使用两个参数的重载
                var method = methods.FirstOrDefault(m => m.GetParameters().Length == 1)
                    ?? methods.FirstOrDefault(m => m.GetParameters().Length == 2);
                
                if (method != null)
                {
                    // 调用扩展方法
                    var parameters = method.GetParameters();
                    var args = new object?[parameters.Length];
                    args[0] = builder; // 第一个参数是 builder
                    
                    // 如果有第二个参数（Action<ConfigCenterOptions>），传 null 使用默认配置
                    if (parameters.Length > 1)
                    {
                        args[1] = null;
                    }
                    
                    method.Invoke(null, args);
                    
                    // ⚠️ 关键：触发配置重新加载，确保配置中心的配置被加载
                    // ConfigurationManager 会自动调用新添加的配置源的 Load() 方法
                    if (builder.Configuration is IConfigurationRoot configRoot)
                    {
                        configRoot.Reload();
                        Console.WriteLine($"[ConfigCenter SDK] 已触发配置重新加载");
                    }
                    
                    Console.WriteLine($"[ConfigCenter SDK] 已自动集成到服务: {serviceName}");
                    
                    // 验证关键配置是否加载成功（可选）
                    var jwtSecret = builder.Configuration["Jwt:SecretKey"];
                    if (string.IsNullOrEmpty(jwtSecret))
                    {
                        Console.WriteLine($"[ConfigCenter SDK] 警告: JWT:SecretKey 配置未找到，将使用本地配置");
                    }
                    else
                    {
                        Console.WriteLine($"[ConfigCenter SDK] JWT 配置已加载: Jwt:SecretKey = {jwtSecret.Substring(0, Math.Min(10, jwtSecret.Length))}...");
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            // 配置中心 SDK 未加载，忽略（某些服务可能不需要配置中心）
        }
        catch (Exception ex)
        {
            // 配置中心 SDK 加载失败，记录警告但不影响应用启动
            Console.WriteLine($"[ConfigCenter SDK] 自动集成失败: {ex.Message}");
        }
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
