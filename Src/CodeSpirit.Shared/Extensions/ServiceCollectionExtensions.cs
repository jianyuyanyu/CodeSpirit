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

            // ���ڿ�����������������������־�Ϳ���̨��־
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
        // ��� HttpContextAccessor ���ڴ滺��
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

        // ע�� AutoMapper
        services.AddAutoMapper(programType);

        // ע��Ȩ�޷���
        services.AddScoped<ICurrentUser, CurrentUser>();

        // ע��ѩ��ID����������
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

        //// ע�� Repositories �� Handlers
        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddDataFilters();
        services.AddCodeSpiritAuthorization();

        services.AddCorsPolicy();

        //ע�� AMIS ����
        services.AddAmisServices(configuration, apiAssembly: programType.Assembly);
        // ��ӷ���ע�ᣬȷ�����������з������ڵĳ���
        //services.AddDependencyInjection(programType.Assembly);
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        //TODO:ͨ�������ļ����ÿ���
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
            // ȫ��ע�� ValidateModelAttribute
            options.Filters.Add<ValidateModelAttribute>();
            options.Filters.Add<HttpResponseExceptionFilter>();
            options.ModelBinderProviders.Insert(0, new DateRangeModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
            optionsAction?.Invoke(options);
        })
        .AddNewtonsoftJson(options =>
        {
            // ��ѡ���ڴ˴����� Newtonsoft.Json ������
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // �������ʱ��ת����
            options.SerializerSettings.Converters.Add(new UTCToLocalDateTimeConverter());

            // ���ö��ת�ַ�����ת��������Ĭ�Ͽ���ö��ӳ�䣬��Ӧ������
            //options.SerializerSettings.Converters.Add(new StringEnumConverter());

            // ��ӳ�����ת�ַ�����ת����
            options.SerializerSettings.Converters.Add(new LongToStringConverter());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // ��ȡ��֤����
                Dictionary<string, string> errors = context.ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key.ToCamelCase(),
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).FirstOrDefault()
                    );

                // ���� Amis ��������Ӧ��ʽ
                var amisResponse = new
                {
                    msg = "��֤�������������",
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
