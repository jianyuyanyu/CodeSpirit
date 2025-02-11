using CodeSpirit.Amis.App;
using CodeSpirit.Amis.Services;
using CodeSpirit.Amis.Validators;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data.Seeders;
using CodeSpirit.IdentityApi.Filters;
using CodeSpirit.IdentityApi.ModelBindings;
using CodeSpirit.IdentityApi.Repositories;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("identity-api");
        Console.WriteLine($"Connection string: {connectionString}");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString)
                   .EnableSensitiveDataLogging()
                   .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole())));

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

        // 注册权限服务
        services.AddScoped<AuthService>();

        // 注册 Repositories 和 Handlers
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ILoginLogRepository, LoginLogRepository>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ILoginLogRepository, LoginLogRepository>();
        services.AddScoped<ILoginLogService, LoginLogService>();

        // 注册 Seeder 类
        services.AddScoped<RoleSeeder>();
        services.AddScoped<UserSeeder>();
        services.AddScoped<SeederService>();

        // 注册 AutoMapper
        services.AddAutoMapper(typeof(Program));

        // 注册自定义授权处理程序
        services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();

        services.AddTransient<IIdentityAccessor, IdentityAccessor>();

        // 注册黑名单服务
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

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
                        .WithOrigins("http://localhost:3000", "https://localhost:7120", "https://*.xin-lai.com")
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
        services.AddControllers(options =>
        {
            // 全局注册 ValidateModelAttribute
            options.Filters.Add<ValidateModelAttribute>();
            options.Filters.Add<HttpResponseExceptionFilter>();
            options.ModelBinderProviders.Insert(0, new DateRangeModelBinderProvider());
        })
        .AddNewtonsoftJson(options =>
        {
            // 可选：在此处配置 Newtonsoft.Json 的设置
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                // 提取验证错误
                Dictionary<string, string> errors = context.ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => ToCamelCase(kvp.Key),
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

    private static string ToCamelCase(string input)
    {
        return string.IsNullOrEmpty(input) || char.IsLower(input[0])
            ? input
            : char.ToLower(input[0], CultureInfo.InvariantCulture) + input.Substring(1);
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
}