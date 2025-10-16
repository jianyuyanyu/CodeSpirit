using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CodeSpirit.Shared.Configuration;

namespace CodeSpirit.Shared.Startup;

/// <summary>
/// API服务配置接口
/// </summary>
public interface IApiServiceConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    string ServiceName { get; }
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    string ConnectionStringKey { get; }
    
    /// <summary>
    /// 路径前缀配置选项
    /// </summary>
    /// <remarks>
    /// 用于配置API服务的路径前缀，支持负载均衡器路由转发
    /// </remarks>
    PathPrefixOptions PathPrefixOptions { get; }
    
    /// <summary>
    /// 配置特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    
    /// <summary>
    /// 配置在认证前的中间件（如多租户中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    Task ConfigurePreAuthenticationMiddlewareAsync(WebApplication app);
    
    /// <summary>
    /// 配置在控制器映射前的中间件（如审计日志中间件）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    Task ConfigurePreControllerMiddlewareAsync(WebApplication app);
    
    /// <summary>
    /// 配置特定中间件（在通用中间件之后）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    Task ConfigureMiddlewareAsync(WebApplication app);
    
    /// <summary>
    /// 数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    Task InitializeDatabaseAsync(WebApplication app);
}
