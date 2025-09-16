using CodeSpirit.ApprovalApi.Data;
using CodeSpirit.ApprovalApi.Services;
using CodeSpirit.ApprovalApi.Services.Settings;
using CodeSpirit.LLM;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeSpirit.AiFormFill;

namespace CodeSpirit.ApprovalApi.Configuration;

/// <summary>
/// 审批系统API服务配置
/// </summary>
public class ApprovalApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "approval";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "approval-api";
    
    /// <summary>
    /// 配置审批系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置多数据库支持的审批系统数据库
        DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<ApprovalDbContext, MySqlApprovalDbContext, SqlServerApprovalDbContext>(
            services, configuration, ConnectionStringKey);
        
        // 注册仓储模式 - 重新注册以确保使用正确的DbContext
        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加HTTP客户端服务
        services.AddHttpClient();
        
        // 添加设置管理服务
        services.AddSettingsManagerWithDatabase(configuration);
        
        // 注册事件总线
        services.AddEventBus();
        
        // 添加审批服务
        services.AddApprovalServices(configuration);
        
        // 添加审批事件处理器
        services.AddApprovalEventHandlers();
        
        // 添加LLM服务（使用设置提供程序）
        services.AddLLMServices<ApprovalLLMSettingsProvider>();
    }
    
    /// <summary>
    /// 配置审批系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 使用多租户中间件
        app.UseCodeSpiritMultiTenant();

        app.UseAiFormFillEndpoints();

        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 审批系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 初始化数据库
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApprovalApiConfiguration>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        
        try
        {
            // 应用数据库迁移（使用改进的迁移方法）
            await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlApprovalDbContext, SqlServerApprovalDbContext>(
                services, configuration, logger, "ApprovalApi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化审批系统数据库时发生错误：{Message}", ex.Message);
            
            // 如果是迁移冲突错误，提供解决建议
            if (ex.Message.Contains("already an object named") || 
                ex.Message.Contains("Table") && ex.Message.Contains("already exists"))
            {
                logger.LogError("检测到数据库迁移冲突！这通常是因为:");
                logger.LogError("1. 数据库中已存在表但迁移历史不一致");
                logger.LogError("2. 多个DbContext尝试创建相同的表");
                logger.LogError("建议解决方案:");
                logger.LogError("1. 运行迁移冲突修复脚本: .\\Scripts\\fix-migration-conflicts.ps1 -ApiProject ApprovalApi -DatabaseType SqlServer -Action CheckStatus");
                logger.LogError("2. 或手动清理数据库: DELETE FROM __EFMigrationsHistory;");
                logger.LogError("3. 然后重启应用程序");
            }
            
            throw;
        }
    }
}