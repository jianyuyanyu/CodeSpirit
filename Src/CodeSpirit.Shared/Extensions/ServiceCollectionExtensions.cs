using CodeSpirit.Amis;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities.Interfaces;
using CodeSpirit.Shared.Filters;
using CodeSpirit.Shared.JsonConverters;
using CodeSpirit.Shared.ModelBindings;
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
        services.AddSingleton<IDataFilter, DataFilter>();
        services.AddSingleton(typeof(IDataFilter<>), typeof(DataFilter<>));
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(ISoftDeleteAuditable)] = new DataFilterState(isEnabled: true);
            options.DefaultStates[typeof(IIsActive)] = new DataFilterState(isEnabled: true);
        });

        return services;
    }

    public static IServiceCollection AddSystemServices(this IServiceCollection services, ConfigurationManager configuration, Type programType, IWebHostEnvironment webHostEnvironment)
    {
        // 添加 HttpContextAccessor 和内存缓存
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        if (webHostEnvironment != null && webHostEnvironment.IsProduction()) {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisConStr");
                options.InstanceName = "CodeSpirit";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // 注册 AutoMapper
        services.AddAutoMapper(programType);

        // 注册权限服务
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 注册雪花ID生成器服务
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

        //// 注册 Repositories 和 Handlers
        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddDataFilters();
        services.AddCodeSpiritAuthorization();

        services.AddCorsPolicy();

        //注册 AMIS 服务
        services.AddAmisServices(configuration, apiAssembly: programType.Assembly);
        // 添加服务注册，确保包含了所有服务所在的程序集
        //services.AddDependencyInjection(programType.Assembly);
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        //TODO:通过配置文件配置跨域
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOriginsWithCredentials",
                builder =>
                {
                    builder
                        .WithOrigins("http://localhost:3000", "https://localhost:7120", "https://*.xin-lai.com", "http://*.xin-lai.com")
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
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
                    status = 422,
                    errors,
                };

                return new BadRequestObjectResult(amisResponse)
                {
                    ContentTypes = { "application/json" }
                };
            };
        });

        return services;
    }
}
