using CodeSpirit.Aggregator;
using CodeSpirit.Amis;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.Shared.Filters;
using CodeSpirit.Shared.JsonConverters;
using CodeSpirit.Shared.ModelBindings;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Services.Background;
using CodeSpirit.Shared.Services.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CodeSpirit.Shared.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase<TDbContext>(this IServiceCollection services, IConfiguration configuration, string appName) where TDbContext : DbContext
    {
        string connectionString = configuration.GetConnectionString(appName);

        services.AddDbContext<DbContext>(options =>
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

    public static IServiceCollection AddDataFilters(this IServiceCollection services)
    {
        services.AddScoped<IDataFilter, DataFilter>();
        services.AddScoped(typeof(IDataFilter<>), typeof(DataFilter<>));
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(ISoftDeleteAuditable)] = new DataFilterState(isEnabled: true);
            options.DefaultStates[typeof(IIsActive)] = new DataFilterState(isEnabled: true);
            options.DefaultStates[typeof(IMultiTenant)] = new DataFilterState(isEnabled: true);
        });

        return services;
    }

    public static IServiceCollection AddSystemServices(this IServiceCollection services, ConfigurationManager configuration, Type programType, IWebHostEnvironment webHostEnvironment)
    {
        // 添加 HttpContextAccessor 和内存缓存
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        // 注册 AutoMapper
        services.AddAutoMapper(programType);

        // 注册权限服务
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 注册雪花ID生成器服务
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

        // 注册客户端IP地址获取服务
        services.AddSingleton<IClientIpService, ClientIpService>();

        //// 注册 Repositories 和 Handlers
        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddDataFilters();
        services.AddCodeSpiritAuthorization();

        services.AddCorsPolicy(configuration);

        //注册 AMIS 服务
        services.AddAmisServices(configuration, apiAssembly: programType.Assembly);
        // 添加服务注册，确保包含了所有服务所在的程序集
        //services.AddDependencyInjection(programType.Assembly);

        services.AddCodeSpiritNavigation();

        services.AddCodeSpiritAggregator(globalConfig =>
        {
            globalConfig.ConfigureCommonGlobalRules();
        });

        // 注册后台任务服务
        services.AddHostedService<BackgroundJobServiceImpl>();
        services.AddSingleton<IBackgroundJobService>(provider =>
            provider.GetRequiredService<IEnumerable<IHostedService>>()
                .OfType<BackgroundJobServiceImpl>()
                .First());

        // 注册文件服务
        services.AddScoped<ITempFileService, TempFileServiceImpl>();
        // 添加唯一性验证服务
        services.AddUniqueValidation();

        services.AddScoped<IImportTemplateService, ImportTemplateService>();

        // 注册增强批量导入助手
        services.AddScoped(typeof(EnhancedBatchImportHelper<>));
         
         return services;
    }

    /// <summary>
    /// 添加跨域策略配置
    /// 支持通过配置文件配置跨域设置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddCors(options =>
        {
            // 从配置文件读取跨域设置
            var corsSection = configuration?.GetSection("Cors");
            var allowedOrigins = corsSection?.GetSection("AllowedOrigins")?.Get<string[]>() 
                ?? new[] { "http://localhost:3000", "https://localhost:7120", "https://*.xin-lai.com", "http://*.xin-lai.com" };
            
            var allowCredentials = corsSection?.GetValue<bool>("AllowCredentials") ?? true;
            var allowAnyHeader = corsSection?.GetValue<bool>("AllowAnyHeader") ?? true;
            var allowAnyMethod = corsSection?.GetValue<bool>("AllowAnyMethod") ?? true;
            var allowWildcardSubdomains = corsSection?.GetValue<bool>("AllowWildcardSubdomains") ?? true;

            options.AddPolicy("AllowSpecificOriginsWithCredentials", builder =>
            {
                // 当启用凭据时，不能使用通配符域名，必须明确指定域名
                if (allowCredentials)
                {
                    // 过滤掉通配符域名，只保留明确的域名
                    var explicitOrigins = allowedOrigins.Where(origin => !origin.Contains("*")).ToArray();
                    if (explicitOrigins.Length > 0)
                    {
                        builder.WithOrigins(explicitOrigins);
                    }
                    else
                    {
                        // 如果没有明确的域名，则使用默认的安全域名
                        builder.WithOrigins("http://localhost:3000", "https://localhost:7120");
                    }
                    
                    builder.AllowCredentials();
                }
                else
                {
                    // 不使用凭据时，可以使用通配符域名
                    builder.WithOrigins(allowedOrigins);
                    
                    if (allowWildcardSubdomains)
                    {
                        builder.SetIsOriginAllowedToAllowWildcardSubdomains();
                    }
                }
                
                if (allowAnyHeader)
                {
                    builder.AllowAnyHeader();
                }
                else
                {
                    var allowedHeaders = corsSection?.GetSection("AllowedHeaders")?.Get<string[]>();
                    if (allowedHeaders?.Length > 0)
                    {
                        builder.WithHeaders(allowedHeaders);
                    }
                }
                
                if (allowAnyMethod)
                {
                    builder.AllowAnyMethod();
                }
                else
                {
                    var allowedMethods = corsSection?.GetSection("AllowedMethods")?.Get<string[]>();
                    if (allowedMethods?.Length > 0)
                    {
                        builder.WithMethods(allowedMethods);
                    }
                }
            });
        });

        return services;
    }

    public static IServiceCollection ConfigureDefaultControllers(this IServiceCollection services, Action<MvcOptions> optionsAction = null)
    {
        services.AddControllers(options =>
        {
            // 全局注册 ValidateModelAttribute
            options.Filters.Add<ValidateModelAttribute>();
            options.Filters.Add<HttpResponseExceptionFilter>();
            options.ModelBinderProviders.Insert(0, new DateRangeModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
            optionsAction?.Invoke(options);
        })
        .AddNewtonsoftJson(options =>
        {
            // 可选：在此处配置 Newtonsoft.Json 的设置
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // 添加日期时间转换器
            options.SerializerSettings.Converters.Add(new UTCToLocalDateTimeConverter());

            // 添加枚举转字符串的转换器（已默认开启枚举映射，不应开启）
            //options.SerializerSettings.Converters.Add(new StringEnumConverter());

            // 添加长整型转字符串的转换器
            options.SerializerSettings.Converters.Add(new LongToStringConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // 提取验证错误
                Dictionary<string, string> errors = context.ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key.ToCamelCase(),
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                    );

                // 构建 Amis 期望的响应格式
                var amisResponse = new
                {
                    msg = "验证错误，请检查输入项！",
                    status = (int)422,
                    errors
                };

                return new BadRequestObjectResult(amisResponse)
                {
                    ContentTypes = { "application/json" }
                };
            };
        });

        return services;
    }

    /// <summary>
    /// 添加共享服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // 添加 HttpContextAccessor
        services.AddHttpContextAccessor();
        
        // 注册客户端IP地址获取服务
        services.AddSingleton<IClientIpService, ClientIpService>();
        
        return services;
    }

    /// <summary>
    /// 添加唯一性验证服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUniqueValidation(this IServiceCollection services)
    {
        services.AddScoped<IUniqueValidationService, UniqueValidationService>();
        return services;
    }
}
