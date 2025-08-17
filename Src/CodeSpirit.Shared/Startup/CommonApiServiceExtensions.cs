using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
// using CodeSpirit.MultiTenant.Extensions; // 注释掉，避免循环引用
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Shared.Startup;

/// <summary>
/// 通用API服务扩展
/// </summary>
public static class CommonApiServiceExtensions
{
    /// <summary>
    /// 添加通用API服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="connectionStringKey">数据库连接字符串键名</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCommonApiServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        string connectionStringKey)
    {
        // 输出连接字符串信息
        var connectionString = configuration.GetConnectionString(connectionStringKey);
        Console.WriteLine($"Connection string ({connectionStringKey}): {connectionString}");
        
        // JWT认证
        services.AddJwtAuthentication(configuration);
        
        // 多租户 - 需要在具体API项目中添加
        // services.AddCodeSpiritMultiTenant(configuration);
        
        // 控制器
        services.ConfigureDefaultControllers();
        
        // 仓储模式
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // AutoMapper - 扫描调用程序集
        var assemblies = GetRelevantAssemblies();
        services.AddAutoMapper(assemblies);
        
        return services;
    }
    
    /// <summary>
    /// 使用通用API中间件（带中间件插入点）
    /// </summary>
    /// <param name="app">Web应用</param>
    /// <param name="config">API配置</param>
    /// <returns>配置后的Web应用</returns>
    public static async Task<WebApplication> UseCommonApiMiddlewareAsync<TConfig>(
        this WebApplication app, 
        TConfig config)
        where TConfig : class, IApiServiceConfiguration
    {
        // CORS
        app.UseCors("AllowSpecificOriginsWithCredentials");
        
        // 插入点1：认证前中间件（如多租户中间件）
        await config.ConfigurePreAuthenticationMiddlewareAsync(app);
        
        // 认证授权
        app.UseAuthentication();
        app.UseAuthorization();
        
        // 插入点2：控制器映射前中间件（如审计日志中间件）
        await config.ConfigurePreControllerMiddlewareAsync(app);
        
        // 控制器映射
        app.MapControllers();
        
        // AMIS UI引擎
        app.UseAmis();
        
        // CodeSpirit权限系统
        app.UseCodeSpiritAuthorization();
        
        // CodeSpirit导航系统
        await app.UseCodeSpiritNavigationAsync();
        
        return app;
    }
    
    /// <summary>
    /// 使用通用API中间件（兼容性方法，无插入点）
    /// </summary>
    /// <param name="app">Web应用</param>
    /// <returns>配置后的Web应用</returns>
    [Obsolete("请使用带配置参数的重载方法以支持中间件插入点")]
    public static async Task<WebApplication> UseCommonApiMiddlewareAsync(
        this WebApplication app)
    {
        // CORS
        app.UseCors("AllowSpecificOriginsWithCredentials");
        
        // 认证授权
        app.UseAuthentication();
        app.UseAuthorization();
        
        // 控制器映射
        app.MapControllers();
        
        // AMIS UI引擎
        app.UseAmis();
        
        // CodeSpirit权限系统
        app.UseCodeSpiritAuthorization();
        
        // CodeSpirit导航系统
        await app.UseCodeSpiritNavigationAsync();
        
        return app;
    }
    
    /// <summary>
    /// 获取相关程序集用于AutoMapper扫描
    /// </summary>
    /// <returns>程序集数组</returns>
    private static Assembly[] GetRelevantAssemblies()
    {
        var assemblies = new List<Assembly>();
        
        // 添加当前执行程序集
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            assemblies.Add(entryAssembly);
        }
        
        // 添加调用程序集
        var callingAssembly = Assembly.GetCallingAssembly();
        if (callingAssembly != null && !assemblies.Contains(callingAssembly))
        {
            assemblies.Add(callingAssembly);
        }
        
        // 添加CodeSpirit相关程序集
        var codeliritAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("CodeSpirit") == true)
            .ToArray();
            
        assemblies.AddRange(codeliritAssemblies);
        
        return assemblies.Distinct().ToArray();
    }
    
    /// <summary>
    /// 通用数据库初始化
    /// </summary>
    /// <typeparam name="TDbContext">数据库上下文类型</typeparam>
    /// <param name="app">Web应用</param>
    /// <param name="seedAction">种子数据操作</param>
    /// <returns>异步任务</returns>
    public static async Task InitializeApiDatabaseAsync<TDbContext>(
        this WebApplication app,
        Func<IServiceProvider, Task>? seedAction = null)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TDbContext>>();
        
        try
        {
            var context = services.GetRequiredService<TDbContext>();
            
            // 执行数据库迁移
            await context.Database.MigrateAsync();
            
            // 执行种子数据操作
            if (seedAction != null)
            {
                await seedAction(services);
            }
            
            logger.LogInformation("数据库初始化完成 - {DbContextType}", typeof(TDbContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化数据库时发生错误 - {DbContextType}", typeof(TDbContext).Name);
            throw;
        }
    }
}
