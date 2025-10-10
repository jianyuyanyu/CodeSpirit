using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Extensions;
using CodeSpirit.ExamApi.Services;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.PdfGeneration.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi.Configuration;

/// <summary>
/// 考试系统API服务配置
/// </summary>
public class ExamApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "exam";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "exam-api";
    
    /// <summary>
    /// 配置考试系统特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置多数据库支持的考试系统数据库
        DatabaseMigrationHelper.ConfigureMultiDatabaseDbContext<ExamDbContext, MySqlExamDbContext, SqlServerExamDbContext>(
            services, configuration, ConnectionStringKey);
        
        // 注册仓储模式
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加Redis分布式锁服务
        AddRedisDistributedLock(services);
        
        // 注册事件总线
        services.AddEventBus();
        
        // 注册Charts服务
        AddChartServices(services);
        
        // 注册AI题目生成和SignalR服务
        AddAIAndSignalRServices(services, configuration);
        
        // 注册PDF生成服务
        services.AddPdfGeneration(configuration);
        
        // 添加设置管理
        services.AddSettingsManagerWithDatabase(configuration);
        
        // 添加HTTP客户端服务
        services.AddHttpClient();
        
        // 配置控制器和审计元数据过滤器
        ConfigureControllersWithAudit(services, configuration);
    }
    
    /// <summary>
    /// 配置控制器和审计元数据过滤器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void ConfigureControllersWithAudit(IServiceCollection services, IConfiguration configuration)
    {
        // 审计元数据过滤器将通过AddAuditMetadataFilter自动注册
        
        // 添加审计元数据过滤器到控制器
        services.AddControllers().AddAuditMetadataFilter();
    }
    
    /// <summary>
    /// 配置考试系统特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 使用多租户中间件
        app.UseCodeSpiritMultiTenant();
        
        // 初始化设置管理
        await app.UseSettingsManagerAsync();
        
        // 使用聚合器
        app.UseCodeSpiritAggregator();
        
        // 使用AI表单填充自动端点
        app.UseAiFormFillEndpoints();
        
        // 初始化PDF生成服务
        await app.UsePdfGenerationAsync();
    }
    
    /// <summary>
    /// 考试系统数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        // 初始化数据库
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ExamApiConfiguration>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        
        try
        {
            // 应用数据库迁移（使用改进的迁移方法）
            await DatabaseMigrationHelper.ApplyDatabaseMigrationsAsync<MySqlExamDbContext, SqlServerExamDbContext>(
                services, configuration, logger, "ExamApi");
            
            // 初始化数据
            var context = services.GetRequiredService<ExamDbContext>();
            await context.InitializeDatabaseAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化考试系统数据库时发生错误：{Message}", ex.Message);
            
            // 如果是迁移冲突错误，提供解决建议
            if (ex.Message.Contains("already an object named") || 
                ex.Message.Contains("Table") && ex.Message.Contains("already exists"))
            {
                logger.LogError("检测到数据库迁移冲突！这通常是因为:");
                logger.LogError("1. 数据库中已存在表但迁移历史不一致");
                logger.LogError("2. 多个DbContext尝试创建相同的表");
                logger.LogError("建议解决方案:");
                logger.LogError("1. 运行迁移冲突修复脚本: .\\Scripts\\fix-migration-conflicts.ps1 -ApiProject ExamApi -DatabaseType SqlServer -Action CheckStatus");
                logger.LogError("2. 或手动清理数据库: DELETE FROM __EFMigrationsHistory;");
                logger.LogError("3. 然后重启应用程序");
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// 添加Redis分布式锁服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddRedisDistributedLock(IServiceCollection services)
    {
        services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:Exam:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
    }
    
    /// <summary>
    /// 添加图表服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddChartServices(IServiceCollection services)
    {
        // 注册Charts服务 - 即使Redis不可用，Chart服务也应该可以使用
        try
        {
            services.AddChartServices(options =>
            {
                options.EnableCache = true;
                options.CacheExpiration = 30;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册Charts服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加AI题目生成和SignalR服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddAIAndSignalRServices(IServiceCollection services, IConfiguration configuration)
    {
        // 使用扩展方法注册AI题目生成相关服务
        services.AddAIQuestionGeneratorServices(configuration);
        
        // 添加AI表单填充服务（包含自动端点功能）
        services.AddAiFormFillEndpoints();
        
        // 添加SignalR服务
        services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
    }
}
