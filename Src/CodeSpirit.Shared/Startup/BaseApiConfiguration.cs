using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Shared.Startup;

/// <summary>
/// 基础API配置抽象类
/// </summary>
public abstract class BaseApiConfiguration : IApiServiceConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public abstract string ServiceName { get; }
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public abstract string ConnectionStringKey { get; }
    
    /// <summary>
    /// 配置特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 默认实现为空，子类可以重写
    }
    
    /// <summary>
    /// 配置在认证前的中间件（如多租户中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public virtual Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app)
    {
        // 默认实现为空，子类可以重写
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 配置在控制器映射前的中间件（如审计日志中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public virtual Task ConfigurePreControllerMiddlewareAsync(WebApplication app)
    {
        // 默认实现为空，子类可以重写
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 配置特定中间件（在通用中间件之后）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public virtual Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 默认实现为空，子类可以重写
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public virtual Task InitializeDatabaseAsync(WebApplication app)
    {
        // 默认实现为空，子类可以重写
        return Task.CompletedTask;
    }
}
