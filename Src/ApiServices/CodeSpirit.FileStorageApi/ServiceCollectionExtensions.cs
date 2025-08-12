using CodeSpirit.Amis;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Options;
using CodeSpirit.FileStorageApi.Providers;
using CodeSpirit.FileStorageApi.Services;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.ServiceDefaults;
// using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeSpirit.FileStorageApi;

/// <summary>
/// 文件存储API服务扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加数据库服务
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("file-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<FileStorageDbContext>(options =>
        {
            options.UseSqlServer(connectionString);

            // 仅在开发环境下启用敏感数据日志和控制台日志
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging()
                       .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            }
        });

        return services;
    }

    public static IServiceCollection AddFileStorage(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults("file");

        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddSystemServices(builder.Configuration, typeof(Program), builder.Environment);
        builder.Services.AddFileStorageApiServices(builder.Configuration);

        // 添加多租户支持
        // builder.Services.AddCodeSpiritMultiTenant(builder.Configuration);

        // 使用共享项目中的JWT认证扩展方法
        builder.Services.AddJwtAuthentication(builder.Configuration);

        builder.Services.ConfigureDefaultControllers();

        // 添加Redis分布式锁服务
        builder.Services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:FileStorage:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });

        return builder.Services;
    }

    /// <summary>
    /// 添加文件存储API服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFileStorageApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 添加 DbContext 基类的解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<FileStorageDbContext>());

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 添加API控制器
        services.AddControllers();

        // 添加AutoMapper
        services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

        // 添加授权
        services.AddAuthorization();

        // 注册事件总线
        services.AddEventBus();

        // 注册文件存储配置
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        // 注册存储提供程序
        services.AddStorageProviders(configuration);

        // 注册业务服务
        services.AddFileStorageServices();

        return services;
    }

    /// <summary>
    /// 添加存储提供程序
    /// </summary>
    public static IServiceCollection AddStorageProviders(this IServiceCollection services, IConfiguration configuration)
    {
        var fileStorageOptions = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>();
        
        if (fileStorageOptions?.StorageProviders != null)
        {
            foreach (var providerConfig in fileStorageOptions.StorageProviders)
            {
                var providerName = providerConfig.Key;
                var providerOptions = providerConfig.Value;

                switch (providerOptions.Type)
                {
                    case StorageProviderType.TencentCOS:
                        // 添加腾讯云COS配置 - 从存储提供程序的Properties中读取
                        services.Configure<TencentCosOptions>(cosOptions =>
                        {
                            var props = providerOptions.Properties;
                            if (props != null)
                            {
                                cosOptions.AppId = props.TryGetValue("AppId", out var appId) ? appId?.ToString() ?? "" : "";
                                cosOptions.SecretId = props.TryGetValue("SecretId", out var secretId) ? secretId?.ToString() ?? "" : "";
                                cosOptions.SecretKey = props.TryGetValue("SecretKey", out var secretKey) ? secretKey?.ToString() ?? "" : "";
                                cosOptions.Region = props.TryGetValue("Region", out var region) ? region?.ToString() ?? "ap-beijing" : "ap-beijing";
                                cosOptions.UseHttps = props.TryGetValue("UseHttps", out var useHttps) && Convert.ToBoolean(useHttps);
                                cosOptions.EnableDebugLog = props.TryGetValue("EnableDebugLog", out var enableDebugLog) && Convert.ToBoolean(enableDebugLog);
                                
                                if (props.TryGetValue("SignatureDurationSeconds", out var signatureDuration))
                                    cosOptions.SignatureDurationSeconds = Convert.ToInt64(signatureDuration);
                                if (props.TryGetValue("ConnectionTimeoutMs", out var connectionTimeout))
                                    cosOptions.ConnectionTimeoutMs = Convert.ToInt32(connectionTimeout);
                                if (props.TryGetValue("ReadWriteTimeoutMs", out var readWriteTimeout))
                                    cosOptions.ReadWriteTimeoutMs = Convert.ToInt32(readWriteTimeout);
                                if (props.TryGetValue("UseTemporaryCredentials", out var useTempCredentials))
                                    cosOptions.UseTemporaryCredentials = Convert.ToBoolean(useTempCredentials);
                            }
                        });
                        break;
                    
                    // 本地存储不需要额外配置
                    case StorageProviderType.Local:
                        break;
                    
                    // TODO: 添加阿里云OSS存储提供程序
                }
            }
        }

        return services;
    }

    /// <summary>
    /// 添加文件存储业务服务
    /// </summary>
    public static IServiceCollection AddFileStorageServices(this IServiceCollection services)
    {
        // 注册存储提供程序工厂
        services.AddSingleton<IStorageProviderFactory, Services.StorageProviderFactory>();
        
        // 注册存储桶配置服务
        services.AddScoped<IBucketConfigurationService, Services.BucketConfigurationService>();
        
        // 注册文件存储服务
        services.AddScoped<IFileStorageService, Services.FileStorageService>();
        
        // 注册文件引用服务
        services.AddScoped<IFileReferenceService, Services.FileReferenceService>();
        
        // 注册图片处理服务
        services.AddScoped<IImageProcessingService, Services.ImageProcessingService>();
        
        // 注册性能监控服务
        services.AddScoped<IFileStorageMetrics, Services.FileStorageMetrics>();

        // 注册系统管理服务
        services.AddSystemServices();

        return services;
    }

    /// <summary>
    /// 添加系统管理服务
    /// </summary>
    public static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        // 注册系统租户存储服务
        services.AddScoped<Services.System.ISystemTenantStorageService, Services.System.SystemTenantStorageService>();
        
        // 注册系统存储桶服务
        services.AddScoped<Services.System.ISystemBucketService, Services.System.SystemBucketService>();
        
        // 注册系统文件服务
        services.AddScoped<Services.System.ISystemFileService, Services.System.SystemFileService>();

        return services;
    }

    /// <summary>
    /// 配置文件存储API服务中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序</returns>
    public static async Task<WebApplication> UseFileStorageApiServicesAsync(this WebApplication app)
    {
        app.UseCors("AllowSpecificOriginsWithCredentials");
        
        // 使用多租户中间件
        // app.UseCodeSpiritMultiTenant();
        
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.UseAmis();
        app.UseCodeSpiritAuthorization();
        await app.UseCodeSpiritNavigationAsync();

        // 初始化数据库
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<FileStorageDbContext>();
                // 使用迁移而不是EnsureCreated
                await context.Database.MigrateAsync();
                // 初始化数据
                await Extensions.FileStorageDbContextExtensions.InitializeDatabaseAsync(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "初始化文件存储数据库时发生错误。");
            }
        }

        return app;
    }
}
