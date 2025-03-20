using CodeSpirit.Aggregator;
using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.ExamApi.Data;
using CodeSpirit.ExamApi.Services.Implementations;
using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;
using CodeSpirit.ExamApi.Services;

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

        return services;
    }

    /// <summary>
    /// 添加图表服务
    /// </summary>
    public static IServiceCollection AddChartServices(this IServiceCollection services)
    {
        // 注册CodeSpirit.Charts服务
        services.AddCharts(options =>
        {
            options.DefaultTheme = "light";
            options.EnableAI = true;
            options.CacheMinutes = 30;
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

        return app;
    }
}