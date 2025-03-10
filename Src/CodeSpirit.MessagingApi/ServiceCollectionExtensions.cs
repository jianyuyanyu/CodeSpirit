using CodeSpirit.Messaging.Data;
using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Messaging.Services;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

/// <summary>
/// 消息系统服务扩展类
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加自定义服务
    /// </summary>
    public static IServiceCollection AddCustomServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<MessagingDbContext>());

        // 注册 Repositories 和服务
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 从已有MessagingServices中迁移 - 保持原有功能
        services.AddMessagingServices(configuration);
        services.AddRealtimeChat();

        return services;
    }

    /// <summary>
    /// 添加数据库服务
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("messaging-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<MessagingDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure());

            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }

    /// <summary>
    /// 添加消息系统服务
    /// </summary>
    public static IServiceCollection AddMessagingApi(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        try
        {
            builder.AddServiceDefaults("messaging");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not add service defaults: {ex.Message}");
        }

        // Add services to the container
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        builder.Services.AddCustomServices(builder.Configuration);
        
        // 添加 JWT 认证
        builder.Services.AddJwtAuthentication(builder.Configuration);
        
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen();

        // 添加 CORS 策略
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOriginsWithCredentials", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithExposedHeaders("Content-Disposition");
            });
        });

        // 添加 SignalR
        builder.Services.AddSignalR()
            .AddNewtonsoftJsonProtocol(options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

        return builder.Services;
    }

    /// <summary>
    /// 配置应用
    /// </summary>
    public static async Task<WebApplication> ConfigureAppAsync(this WebApplication app)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowSpecificOriginsWithCredentials");
        
        // 添加身份验证中间件
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();
        app.MapHub<ChatHub>("/chathub");

        // 应用迁移
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("数据库迁移应用成功！");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用迁移时发生错误: {ex.Message}");
        }

        // Map default endpoints if available
        try
        {
            app.MapDefaultEndpoints();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not map default endpoints: {ex.Message}");
        }

        return app;
    }
}