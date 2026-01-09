using CodeSpirit.Aggregator;
using CodeSpirit.AiFormFill;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Caching.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Extensions;
using CodeSpirit.ExamApi.Services;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.ExamApi.Tasks;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.ScheduledTasks.Extensions;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Performance;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
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
        // 配置线程池（高并发服务等级）
        var logger = services.BuildServiceProvider().GetService<ILoggerFactory>()?.CreateLogger<ExamApiConfiguration>();
        ThreadPoolConfiguration.ConfigureThreadPool(ThreadPoolConfiguration.ServiceTier.High, expectedInstances: 3, logger);
        
        // 调用基类方法以初始化路径前缀配置
        base.ConfigureServices(services, configuration);
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
        
        // 注册 QuestPDF 服务（轻量级PDF生成，默认启用）
        AddQuestPdfServices(services);
        
        // 添加设置管理
        services.AddSettingsManagerWithDatabase(configuration);
        
        // 添加HTTP客户端服务
        services.AddHttpClient();
        
        // 添加输出缓存服务
        AddOutputCacheServices(services);
        
        // 添加CodeSpirit缓存服务
        AddCachingServices(services, configuration);
        
        // 添加定时任务服务
        AddScheduledTasksServices(services, configuration);

        // 🚨 紧急优化：添加请求限流保护
        AddRateLimiterServices(services);
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
        
        // 🚨 紧急优化：启用请求限流中间件（必须在路由之前）
        app.UseRateLimiter();
        
        // 使用输出缓存中间件
        app.UseOutputCache();
        
        // 初始化设置管理
        await app.UseSettingsManagerAsync();
        
        // 使用聚合器
        app.UseCodeSpiritAggregator();
        
        // 使用AI表单填充自动端点
        app.UseAiFormFillEndpoints();
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
    /// 添加输出缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddOutputCacheServices(IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            // 配置默认缓存策略
            options.AddBasePolicy(builder => 
            {
                builder.Expire(TimeSpan.FromMinutes(5)); // 默认5分钟过期
                
                // ⚠️ 重要安全控制：只缓存已认证用户的请求
                // 作用：避免缓存401 Unauthorized响应
                builder.With(context => 
                {
                    return context.HttpContext.User.Identity?.IsAuthenticated == true;
                });
            });
            
            // 📝 ASP.NET Core OutputCache默认行为说明：
            // ✅ 只缓存成功响应（200-299状态码）
            // ❌ 不缓存客户端错误（4xx：401, 404, 400等）
            // ❌ 不缓存服务器错误（5xx：500, 502, 503等）
            // ❌ 不缓存重定向响应（3xx）
            // 
            // 通过builder.With()我们进一步限制：
            // ✅ 只缓存已认证用户的成功响应
            // ❌ 未认证用户的401响应不会被缓存
        });
        
        Console.WriteLine("已配置HTTP输出缓存：仅缓存已认证用户的成功响应（2xx）");
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
    
    /// <summary>
    /// 添加考试API特定的缓存服务
    /// 注意：通用缓存服务已在CommonApiServices中统一配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddCachingServices(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // 注册考试API特定的缓存服务
            services.AddScoped<IExamCacheService, ExamCacheService>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册考试缓存服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加定时任务服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private void AddScheduledTasksServices(IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // 注册定时任务组件
            services.AddCodeSpiritScheduledTasks(configuration, ServiceName);
            
            // 注册考试缓存预热任务处理器
            services.AddTaskHandler<ExamCacheWarmupTaskHandler>();
            
            // ✅ 注册考试记录预生成任务处理器（用于手动触发）
            services.AddTaskHandler<ExamRecordPreGenerationTaskHandler>();
            
            // ✅ 注册考试记录定时预生成任务处理器（每天凌晨1点执行）
            services.AddTaskHandler<ExamRecordScheduledPreGenerationTaskHandler>();
            
            // ✅ 注册考试记录清理任务处理器
            services.AddTaskHandler<ExamRecordCleanupTaskHandler>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册定时任务服务时出错: {ex.Message}，但应用程序将继续启动");
        }
    }
    
    /// <summary>
    /// 添加 QuestPDF 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddQuestPdfServices(IServiceCollection services)
    {
        try
        {
            // 配置 QuestPDF 许可证（Community版）
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            
            // 注册 QuestPDF 生成服务
            services.AddScoped<Services.PdfGeneration.IQuestPdfGenerationService, Services.PdfGeneration.QuestPdfGenerationService>();
            
            // 输出字体配置信息
            var font = Services.PdfGeneration.FontHelper.GetChineseFont();
            var fallback = Services.PdfGeneration.FontHelper.GetFallbackFont();
            var symbolFont = Services.PdfGeneration.FontHelper.GetSymbolFont();
            var os = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : "Other";
            
            Console.WriteLine($"已配置 QuestPDF 服务：许可证=Community，操作系统={os}");
            Console.WriteLine($"  主字体={font}，后备字体={fallback}，符号字体={symbolFont}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册 QuestPDF 服务时出错: {ex.Message}，PDF导出功能可能不可用");
        }
    }
    
    /// <summary>
    /// 添加请求限流服务 - 紧急优化
    /// </summary>
    /// <param name="services">服务集合</param>
    private static void AddRateLimiterServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 全局固定窗口限流策略 - 基于用户身份
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
            {
                // 优先使用用户ID（从JWT Token中获取），避免多用户共享IP的问题
                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                             ?? context.User?.FindFirst("sub")?.Value
                             ?? context.User?.FindFirst("userId")?.Value;
                
                // 如果未登录，则基于IP限流（匿名访问场景）
                var partitionKey = userId ?? $"anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
                
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200, // 每个用户每10秒允许200个请求（适应正常使用场景）
                        Window = TimeSpan.FromSeconds(10), // 10秒窗口
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = 100 // 队列最多100个请求
                    });
            });
            
            // 添加特定端点的限流策略（针对高频考试API）
            options.AddPolicy("client-api", context =>
            {
                // 基于用户ID限流
                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                             ?? context.User?.FindFirst("sub")?.Value
                             ?? context.User?.FindFirst("userId")?.Value;
                
                var partitionKey = userId ?? $"anonymous:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
                
                return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100, // 客户端API每10秒最多100个请求（考试场景频繁保存答案）
                        Window = TimeSpan.FromSeconds(10),
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = 50
                    });
            });
            
            // 拒绝请求时的响应
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                await context.HttpContext.Response.WriteAsync(
                    "{\"status\":-1,\"msg\":\"请求过于频繁，请稍后再试\"}",
                    cancellationToken);
            };
        });
        
        Console.WriteLine("已配置请求限流：基于用户ID，全局200次/10秒，客户端API 100次/10秒");
    }
}
