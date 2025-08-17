using CodeSpirit.FileStorageApi.Abstractions;
using CodeSpirit.FileStorageApi.Data;
using CodeSpirit.FileStorageApi.EventHandlers;
using CodeSpirit.FileStorageApi.Extensions;
using CodeSpirit.FileStorageApi.Options;
using CodeSpirit.FileStorageApi.Providers;
using CodeSpirit.FileStorageApi.Services;
using CodeSpirit.MultiTenant.Extensions;
using CodeSpirit.Shared.DistributedLock;
using CodeSpirit.Shared.EventBus.Events;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Repositories;
using CodeSpirit.Shared.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeSpirit.FileStorageApi.Configuration;

/// <summary>
/// 文件存储API服务配置
/// </summary>
public class FileStorageApiConfiguration : BaseApiConfiguration
{
    /// <summary>
    /// 服务名称，用于Aspire服务发现
    /// </summary>
    public override string ServiceName => "file";
    
    /// <summary>
    /// 数据库连接字符串键名
    /// </summary>
    public override string ConnectionStringKey => "file-api";
    
    /// <summary>
    /// 配置文件存储特定服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 配置文件存储数据库
        var connectionString = configuration.GetConnectionString(ConnectionStringKey);
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
        
        // 注册DbContext基类解析
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<FileStorageDbContext>());
        
        // 添加多租户支持
        services.AddCodeSpiritMultiTenant(configuration);
        
        // 添加控制器和授权
        services.AddControllers();
        services.AddAuthorization();
        
        // 添加AutoMapper
        services.AddAutoMapper(typeof(FileStorageApiConfiguration).Assembly);
        
        // 添加Redis分布式锁服务
        services.AddRedisDistributedLock(options =>
        {
            options.KeyPrefix = "CodeSpirit:FileStorage:Lock:";
            options.DefaultLockTimeout = TimeSpan.FromMinutes(5);
            options.DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
            options.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
        
        // 注册事件总线
        services.AddEventBus();
        
        // 注册文件存储配置
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        
        // 注册存储提供程序
        AddStorageProviders(services, configuration);

        // 注册文件引用事件处理器
        services.AddTenantAwareEventHandler<FileReferenceEvent, FileReferenceEventHandler>();
    }
    
    /// <summary>
    /// 配置文件存储特定中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override Task ConfigureMiddlewareAsync(WebApplication app)
    {
        // 配置静态文件托管 - 用于图片等文件访问
        ConfigureStaticFiles(app);

        // 使用多租户中间件
        app.UseCodeSpiritMultiTenant();
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 文件存储数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>异步任务</returns>
    public override async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<FileStorageDbContext>();
            // 使用迁移而不是EnsureCreated
            await context.Database.MigrateAsync();
            // 初始化数据
            await FileStorageDbContextExtensions.InitializeDatabaseAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<FileStorageApiConfiguration>>();
            logger.LogError(ex, "初始化文件存储数据库时发生错误");
            throw;
        }
    }
    
    /// <summary>
    /// 添加存储提供程序
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    private static void AddStorageProviders(IServiceCollection services, IConfiguration configuration)
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
    }
    
    /// <summary>
    /// 配置静态文件托管
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    private static void ConfigureStaticFiles(WebApplication app)
    {
        // 获取文件存储配置
        var fileStorageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        
        // 配置静态文件中间件
        if (fileStorageOptions.StorageProviders.TryGetValue("Local", out var localProvider))
        {
            var properties = localProvider.Properties;
            if (properties.TryGetValue("RootPath", out var rootPath) && rootPath is string rootPathStr)
            {
                var uploadsPath = Path.Combine(app.Environment.ContentRootPath, rootPathStr);
                
                // 确保上传目录存在
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                
                // 配置静态文件中间件，将 /files 路径映射到上传目录
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(uploadsPath),
                    RequestPath = "/files",
                    ServeUnknownFileTypes = true, // 允许提供未知文件类型
                    DefaultContentType = "application/octet-stream",
                    OnPrepareResponse = context =>
                    {
                        // 设置缓存头以优化性能
                        var headers = context.Context.Response.Headers;
                        headers.CacheControl = "public,max-age=31536000"; // 1年缓存
                        headers.Expires = DateTime.UtcNow.AddYears(1).ToString("R");
                        
                        // 为图片文件设置正确的MIME类型
                        var fileExtension = Path.GetExtension(context.File.Name).ToLowerInvariant();
                        switch (fileExtension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                context.Context.Response.ContentType = "image/jpeg";
                                break;
                            case ".png":
                                context.Context.Response.ContentType = "image/png";
                                break;
                            case ".gif":
                                context.Context.Response.ContentType = "image/gif";
                                break;
                            case ".webp":
                                context.Context.Response.ContentType = "image/webp";
                                break;
                            case ".svg":
                                context.Context.Response.ContentType = "image/svg+xml";
                                break;
                            case ".bmp":
                                context.Context.Response.ContentType = "image/bmp";
                                break;
                            case ".tiff":
                            case ".tif":
                                context.Context.Response.ContentType = "image/tiff";
                                break;
                        }
                    }
                });
                
                app.Logger.LogInformation("静态文件托管已配置: /files -> {UploadsPath}", uploadsPath);
            }
        }
    }
}
