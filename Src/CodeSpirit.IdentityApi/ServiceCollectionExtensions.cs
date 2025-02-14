using Audit.Core;
using Audit.WebApi;
using CodeSpirit.Amis;
using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Services;
using CodeSpirit.Amis.Validators;
using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.Core.Extensions;
using CodeSpirit.Core.IdGenerator;
using CodeSpirit.IdentityApi.Audit;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Repositories;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.DependencyInjection;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Filters;
using CodeSpirit.Shared.JsonConverters;
using CodeSpirit.Shared.ModelBindings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("identity-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ApplicationDbContext>(options =>
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
            options.DefaultStates[typeof(IDeletionAuditedObject)] = new DataFilterState(isEnabled: true);
            options.DefaultStates[typeof(ITenant)] = new DataFilterState(isEnabled: true);
            options.DefaultStates[typeof(IIsActive)] = new DataFilterState(isEnabled: true);
        });

        return services;
    }

    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // 添加 HttpContextAccessor 和内存缓存
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        // 添加服务注册，确保包含了所有服务所在的程序集
        services.AddDependencyInjection(typeof(Program).Assembly);

        // 注册 AutoMapper
        services.AddAutoMapper(typeof(Program));

        // 注册权限服务
        services.AddScoped<ICurrentUser, CurrentUser>();

        // 注册雪花ID生成器服务
        services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        // 注册自定义授权处理程序（这个需要特殊处理，因为是 Identity 框架的组件）
        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // 密码设置
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // 锁定设置
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // 用户设置
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
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

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddCodeSpiritAuthorization();
        return services;
    }

    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        // 注册 FluentValidation 验证器
        // services.AddValidatorsFromAssemblyContaining<PageValidator>();

        // 注册特定验证器
        services.AddTransient<IValidator<Page>, PageValidator>();
        services.AddScoped<IPageCollector, PageCollector>();

        return services;
    }

    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        // 配置审计
        Audit.Core.Configuration.Setup()
            .UseCustomProvider(new CustomAuditDataProvider(serviceProvider: services.BuildServiceProvider()))
            .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

        services.AddControllers(options =>
        {
            // 全局注册 ValidateModelAttribute
            options.Filters.Add<ValidateModelAttribute>();
            options.Filters.Add<HttpResponseExceptionFilter>();
            options.ModelBinderProviders.Insert(0, new DateRangeModelBinderProvider());

            // 修改审计过滤器配置
            options.AddAuditFilter(config => config
                .LogAllActions()
                .WithEventType("{verb}.{controller}.{action}")
                .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                .IncludeRequestBody()
                .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode != 200)
                .IncludeModelState()
                .SerializeActionParameters()
            );
        })
        .AddNewtonsoftJson(options =>
        {
            // 可选：在此处配置 Newtonsoft.Json 的设置
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            // 添加长整型转字符串的转换器
            options.SerializerSettings.Converters.Add(new StringEnumConverter());
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

    /// <summary>
    /// 添加Jwt认证
    /// </summary>
    /// <param name="builder"></param>
    public static void AddJwtAuthentication(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.IncludeErrorDetails = true;
            //用于指定是否将从令牌提取的数据保存到当前的安全上下文中。当设置为true时，可以通过HttpContext.User.Claims来访问这些数据。
            //如果不需要使用从令牌提取的数据，可以将该属性设置为false以节省系统资源和提高性能。
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Jwt:SecretKey"])),
                ClockSkew = TimeSpan.Zero, // 设置时钟偏移量为0，即不允许过期的Token被接受
                RequireExpirationTime = true, // 要求Token必须有过期时间
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                NameClaimType = "id"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    ITokenBlacklistService blacklistService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenBlacklistService>();

                    // 获取原始令牌
                    string token = context.SecurityToken.ToString();

                    // 检查令牌是否在黑名单中
                    if (await blacklistService.IsBlacklistedAsync(token))
                    {
                        context.Fail("令牌已被禁用！");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            new ApiResponse(401, "令牌已被禁用，请重新登录！"));
                        return;
                    }

                    return;
                }
            };
        });
    }

    public static WebApplicationBuilder AddIdentityApiServices(this WebApplicationBuilder builder)
    {
        // Add service defaults & Aspire client integrations
        builder.AddServiceDefaults("identity-api");

        // Add services to the container
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddDataFilters();
        builder.Services.AddCustomServices();
        builder.Services.AddIdentityServices();
        builder.Services.AddCorsPolicy();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddAuthorizationPolicies();
        builder.Services.AddFluentValidationServices();
        builder.Services.ConfigureControllers();
        builder.Services.AddAmisServices(builder.Configuration, apiAssembly: typeof(Program).Assembly);

        // 配置审计
        builder.Services.Configure<AuditConfig>(
            builder.Configuration.GetSection("Audit"));

        return builder;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        // 执行数据初始化
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            ILogger<SeederService> logger = services.GetRequiredService<ILogger<SeederService>>();
            try
            {
                // 调用数据初始化方法
                await DataSeeder.SeedAsync(services, logger);
            }
            catch (Exception ex)
            {
                // 在控制台输出错误
                logger.LogError(ex, $"数据初始化失败：{ex.Message}");
                throw;
            }
        }
    }

    public static WebApplication ConfigureApp(this WebApplication app)
    {
        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAuditLogging();
        app.MapControllers();
        app.UseAmis();
        return app;
    }
}