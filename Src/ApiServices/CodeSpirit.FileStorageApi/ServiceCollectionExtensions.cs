using CodeSpirit.ServiceDefaults;
using CodeSpirit.Authorization.Extensions;
// using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Services;
using CodeSpirit.FileStorageApi.Providers;
using CodeSpirit.FileStorageApi.Options;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.FileStorageApi;

/// <summary>
/// 文件存储API服务扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults("CodeSpirit.FileStorageApi");

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
        
        string connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
            "Server=(LocalDB)\\MSSQLLocalDB;Database=CodeSpirit.FileStorage;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
        Console.WriteLine($"FileStorage Connection string: {connectionString}");

        services.AddDbContext<FileStorageDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // 添加AutoMapper
        // services.AddAutoMapper(cfg => {}, typeof(ServiceCollectionExtensions).Assembly);

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
                    case StorageProviderType.Local:
                        services.AddScoped<IStorageProvider>(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<LocalStorageProvider>>();
                            return new LocalStorageProvider(providerOptions, logger);
                        });
                        break;
                    
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
                        services.AddScoped<IStorageProvider, TencentCosStorageProvider>();
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
        app.UseCodeSpiritAuthorization();

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
