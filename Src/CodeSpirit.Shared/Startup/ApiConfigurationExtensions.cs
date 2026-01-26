using CodeSpirit.Aggregator;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.Startup;

/// <summary>
/// API配置扩展方法
/// </summary>
/// <remarks>
/// 提供简化的配置方法，减少配置类中的重复代码
/// </remarks>
public static class ApiConfigurationExtensions
{
    /// <summary>
    /// 配置标准数据库服务
    /// </summary>
    /// <typeparam name="TDbContext">主数据库上下文类型</typeparam>
    /// <typeparam name="TMySqlDbContext">MySQL数据库上下文类型</typeparam>
    /// <typeparam name="TSqlServerDbContext">SQL Server数据库上下文类型</typeparam>
    /// <param name="config">API配置实例</param>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <remarks>
    /// 自动配置：
    /// - 多数据库支持（MySQL/SQL Server）
    /// - 仓储模式注册
    /// </remarks>
    public static void ConfigureStandardDatabaseServices<TDbContext, TMySqlDbContext, TSqlServerDbContext>(
        this BaseApiConfiguration config,
        IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
        where TMySqlDbContext : DbContext
        where TSqlServerDbContext : DbContext
    {
        // 配置多数据库支持
        DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<TDbContext, TMySqlDbContext, TSqlServerDbContext>(
            services, configuration, config.ConnectionStringKey);
        
        // 注册仓储模式
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    }
    
    /// <summary>
    /// 配置标准基础设施服务
    /// </summary>
    /// <param name="config">API配置实例</param>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="additionalConfiguration">额外的服务配置委托（可选）</param>
    /// <remarks>
    /// 自动配置：
    /// - 事件总线
    /// - HTTP客户端
    /// 
    /// 通过 additionalConfiguration 参数可配置可选组件：
    /// - 多租户支持 (AddCodeSpiritMultiTenant)
    /// - 设置管理 (AddSettingsManagerWithDatabase)
    /// </remarks>
    public static void ConfigureStandardInfrastructureServices(
        this BaseApiConfiguration config,
        IServiceCollection services,
        IConfiguration configuration,
        Action<IServiceCollection, IConfiguration>? additionalConfiguration = null)
    {
        // 注册事件总线
        services.AddEventBus();
        
        // 添加HTTP客户端服务
        services.AddHttpClient();
        
        // 执行额外的服务配置（由各配置类自行决定需要哪些可选组件）
        additionalConfiguration?.Invoke(services, configuration);
    }
    
    /// <summary>
    /// 配置标准中间件
    /// </summary>
    /// <param name="config">API配置实例</param>
    /// <param name="app">应用程序构建器</param>
    /// <param name="additionalMiddleware">额外的中间件配置委托（可选）</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 自动配置：
    /// - 聚合器中间件
    /// 
    /// 通过 additionalMiddleware 参数可配置可选组件：
    /// - 多租户中间件 (UseCodeSpiritMultiTenant)
    /// - AI表单填充端点 (UseAiFormFillEndpoints)
    /// </remarks>
    public static Task ConfigureStandardMiddlewareAsync(
        this BaseApiConfiguration config,
        WebApplication app,
        Action<WebApplication>? additionalMiddleware = null)
    {
        // 使用聚合器
        app.UseCodeSpiritAggregator();
        
        // 执行额外的中间件配置（由各配置类自行决定需要哪些可选组件）
        additionalMiddleware?.Invoke(app);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 标准数据库初始化
    /// </summary>
    /// <typeparam name="TDbContext">主数据库上下文类型</typeparam>
    /// <typeparam name="TMySqlDbContext">MySQL数据库上下文类型</typeparam>
    /// <typeparam name="TSqlServerDbContext">SQL Server数据库上下文类型</typeparam>
    /// <param name="config">API配置实例</param>
    /// <param name="app">应用程序构建器</param>
    /// <param name="apiName">API名称，用于日志记录</param>
    /// <returns>异步任务</returns>
    /// <remarks>
    /// 自动执行：
    /// - 应用数据库迁移
    /// - 初始化种子数据（如果 DbContext 实现了 IInitializableDbContext）
    /// - 统一的错误处理和日志记录
    /// </remarks>
    public static async Task InitializeStandardDatabaseAsync<TDbContext, TMySqlDbContext, TSqlServerDbContext>(
        this BaseApiConfiguration config,
        WebApplication app,
        string apiName)
        where TDbContext : DbContext
        where TMySqlDbContext : DbContext
        where TSqlServerDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(config.GetType());
        var configuration = services.GetRequiredService<IConfiguration>();
        
        try
        {
            // 应用数据库迁移
            await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<TMySqlDbContext, TSqlServerDbContext>(
                services, configuration, logger, apiName);
            
            // 初始化种子数据（如果 DbContext 实现了 IInitializableDbContext）
            var context = services.GetRequiredService<TDbContext>();
            if (context is IInitializableDbContext initializable)
            {
                await initializable.InitializeDatabaseAsync();
                logger.LogInformation("{ApiName} 数据库种子数据初始化完成", apiName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化 {ApiName} 数据库时发生错误：{Message}", apiName, ex.Message);
            
            // 如果是迁移冲突错误，提供解决建议
            if (ex.Message.Contains("already an object named") || 
                ex.Message.Contains("Table") && ex.Message.Contains("already exists"))
            {
                logger.LogError("检测到数据库迁移冲突！这通常是因为:");
                logger.LogError("1. 数据库中已存在表但迁移历史不一致");
                logger.LogError("2. 多个DbContext尝试创建相同的表");
                logger.LogError("建议解决方案:");
                logger.LogError("1. 运行迁移冲突修复脚本: .\\Scripts\\fix-migration-conflicts.ps1 -ApiProject {ApiProject} -DatabaseType SqlServer -Action CheckStatus", apiName);
                logger.LogError("2. 或手动清理数据库: DELETE FROM __EFMigrationsHistory;");
                logger.LogError("3. 然后重启应用程序");
            }
            
            throw;
        }
    }
}
