using CodeSpirit.Aggregator;
using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.ExamApi.Services;
using CodeSpirit.ExamApi.Services.TextParsers.v2;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.PdfGeneration.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Settings.Extensions;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using Newtonsoft.Json;

namespace CodeSpirit.ExamApi;

/// <summary>
/// 考试系统API服务扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExam(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults("config");

        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        builder.Services.AddExamApiServices(builder.Configuration);

        // 使用共享项目中的JWT认证扩展方法
        builder.Services.AddJwtAuthentication(builder.Configuration);

        builder.Services.ConfigureDefaultControllers();

        // 添加Redis分布式锁服务
        builder.Services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:Exam:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });

        return builder.Services;
    }

    /// <summary>
    /// 添加考试系统API服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddExamApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ExamDbContext>());

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 添加API控制器
        services.AddControllers();
        
        string connectionString = configuration.GetConnectionString("exam-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ExamDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // 添加AutoMapper
        services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

        // 添加授权
        services.AddAuthorization();

        // 注册事件总线
        services.AddEventBus();
        
        // 注册Charts服务 - 即使Redis不可用，Chart服务也应该可以使用
        try
        {
            services.AddChartServices();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"警告: 注册Charts服务时出错: {ex.Message}，但应用程序将继续启动");
        }

        // 注册服务
        // 注册具体的解析器
        services.AddScoped<SingleChoiceQuestionParser>();
        services.AddScoped<MultipleChoiceQuestionParser>();
        services.AddScoped<TrueFalseQuestionParser>();

        // 注册主解析器
        services.AddScoped<QuestionTextParserV2>();

        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IStudentGroupService, StudentGroupService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IExamPaperService, ExamPaperService>();
        services.AddScoped<IExamRecordService, ExamRecordService>();
        services.AddScoped<IExamStatisticsService, ExamStatisticsService>();
        services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();
        services.AddScoped<IWrongQuestionService, WrongQuestionService>();
        services.AddScoped<IQuestionVersionService, QuestionVersionService>();
        services.AddScoped<IPracticeRecordService, PracticeRecordService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IExamSettingService, ExamSettingService>();
        services.AddScoped<IMonitorService, MonitorService>();

        // 注册PDF生成服务
        services.AddPdfGeneration(configuration);

        // 注册AI题目生成服务
        services.AddSignalRAndNotificationServices();
        services.AddHttpClient();

        // 添加设置管理
        services.AddSettingsManagerWithDatabase(configuration);
        
        return services;
    }

    /// <summary>
    /// 添加图表服务
    /// </summary>
    public static IServiceCollection AddChartServices(this IServiceCollection services)
    {
        // 注册CodeSpirit.Charts服务
        services.AddChartServices(options =>
        {
            options.EnableCache = true;
            options.CacheExpiration = 30;
        });

        return services;
    }

    /// <summary>
    /// 添加AI题目生成服务
    /// </summary>
    public static IServiceCollection AddSignalRAndNotificationServices(this IServiceCollection services)
    {
        // 注册AI题目生成服务
        services.AddScoped<IAIQuestionGeneratorService, AIQuestionGeneratorService>();
        
        // 注册通知服务
        services.AddScoped<IGeneratorNotificationService, GeneratorNotificationService>();
        
        // 添加SignalR服务
        services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });
            
        return services;
    }

    /// <summary>
    /// 配置考试系统API服务中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序</returns>
    public static async Task<WebApplication> UseExamApiServicesAsync(this WebApplication app)
    {
        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseAmis();
        app.UseCodeSpiritAuthorization();
        await app.UseCodeSpiritNavigationAsync();
        
        // 初始化设置管理
        await app.UseSettingsManagerAsync();
        
        app.UseCodeSpiritAggregator();

        // 初始化数据库
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ExamDbContext>();
                // 使用迁移而不是EnsureCreated
                await context.Database.MigrateAsync();
                // 初始化数据
                await context.InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "初始化数据库时发生错误。");
            }
        }

        // 初始化PDF生成服务
        await app.UsePdfGenerationAsync();

        return app;
    }
}