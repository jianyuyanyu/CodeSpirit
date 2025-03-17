using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;

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
        
        // 添加身份验证
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
                };
            });
        
        // 添加授权
        services.AddAuthorization();
        
        //// 注册仓储
        //services.AddScoped<IQuestionRepository, QuestionRepository>();
        //services.AddScoped<IQuestionCategoryRepository, QuestionCategoryRepository>();
        //services.AddScoped<IStudentRepository, StudentRepository>();
        //services.AddScoped<IStudentGroupRepository, StudentGroupRepository>();
        //services.AddScoped<IPracticeRecordRepository, PracticeRecordRepository>();
        //services.AddScoped<IWrongQuestionRepository, WrongQuestionRepository>();
        //services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
        //services.AddScoped<IExamSettingRepository, ExamSettingRepository>();
        //services.AddScoped<IExamRecordRepository, ExamRecordRepository>();
        
        //// 注册服务
        //services.AddScoped<IQuestionService, QuestionService>();
        //services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();
        //services.AddScoped<IStudentService, StudentService>();
        //services.AddScoped<IStudentGroupService, StudentGroupService>();
        //services.AddScoped<IPracticeService, PracticeService>();
        //services.AddScoped<IWrongQuestionService, WrongQuestionService>();
        //services.AddScoped<IExamPaperService, ExamPaperService>();
        //services.AddScoped<IExamSettingService, ExamSettingService>();
        //services.AddScoped<IExamService, ExamService>();
        
        return services;
    }
    
    /// <summary>
    /// 配置考试系统API服务中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序</returns>
    public static WebApplication UseExamApiServices(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
        return app;
    }
}