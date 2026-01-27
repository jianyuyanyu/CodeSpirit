using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.Shared.Configuration;

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
    /// 路径前缀配置选项
    /// </summary>
    /// <remarks>
    /// 默认实现从配置文件和环境变量中读取PathPrefix配置节
    /// 子类可以重写此属性以提供自定义配置
    /// </remarks>
    public virtual PathPrefixOptions PathPrefixOptions { get; protected set; } = new();
    
    /// <summary>
    /// 配置特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 初始化路径前缀配置
        InitializePathPrefixOptions(configuration);
        
        // 自动配置审计元数据过滤器（如果启用）
        ConfigureAuditMetadataFilter(services, configuration);
        
        // 默认实现为空，子类可以重写
    }
    
    /// <summary>
    /// 配置审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <remarks>
    /// 默认实现为空。需要审计功能的 API 服务应继承 
    /// <see cref="CodeSpirit.Audit.Startup.AuditAwareApiConfiguration"/>，
    /// 该类会自动配置审计元数据过滤器。
    /// </remarks>
    protected virtual void ConfigureAuditMetadataFilter(IServiceCollection services, IConfiguration configuration)
    {
        // 默认实现为空，由 AuditAwareApiConfiguration 子类提供具体实现
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
    
    /// <summary>
    /// 初始化路径前缀配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <remarks>
    /// 从配置文件和环境变量中读取PathPrefix配置节
    /// 环境变量优先级高于配置文件
    /// </remarks>
    protected virtual void InitializePathPrefixOptions(IConfiguration configuration)
    {
        var options = new PathPrefixOptions();
        
        // 绑定配置文件中的PathPrefix节
        configuration.GetSection(PathPrefixOptions.SectionName).Bind(options);
        
        // 环境变量会自动覆盖配置文件中的值（通过ASP.NET Core的配置系统）
        PathPrefixOptions = options;
        
        // 验证配置的有效性
        var validationResult = PathPrefixOptions.Validate();
        if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
        {
            throw new InvalidOperationException($"路径前缀配置无效: {validationResult.ErrorMessage}");
        }
    }
}
