using CodeSpirit.IdentityApi.Authorization;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Filters;
using CodeSpirit.IdentityApi.MappingProfiles;
using CodeSpirit.IdentityApi.Repositories;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Entities;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// 添加数据库上下文和 Identity 服务
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("identity-api"))
    .EnableSensitiveDataLogging()
    .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole())));

builder.Services.AddSingleton<IDataFilter, DataFilter>();
builder.Services.AddSingleton(typeof(IDataFilter<>), typeof(DataFilter<>));
builder.Services.Configure<DataFilterOptions>(options =>
{
    options.DefaultStates[typeof(IDeletionAuditedObject)] = new DataFilterState(isEnabled: true);
    options.DefaultStates[typeof(ITenant)] = new DataFilterState(isEnabled: true);
    options.DefaultStates[typeof(IIsActive)] = new DataFilterState(isEnabled: true);
});
builder.Services.AddSingleton<AmisGenerator>(provider =>
{
    return new AmisGenerator(Assembly.GetExecutingAssembly());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IIdentityAccessor, IdentityAccessor>();
////依赖注入驱动注册
//builder.Services.AddScopedRegister<IScopedDependency>();
//builder.Services.AddTransientRegister<ITransientDependency>();
//builder.Services.AddSingletonRegister<ISingletonDependency>();

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOriginsWithCredentials",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000") // 前端应用的地址
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // 允许凭证
        });
});


//// 配置 JWT 认证
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        ValidAudience = builder.Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
//    };
//});

// 注册分布式缓存服务（如使用内存缓存）
builder.Services.AddDistributedMemoryCache();

// 注册自定义授权处理程序
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<SignInManager<ApplicationUser>, CustomSignInManager>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
// 注册 AutoMapper 并扫描指定的程序集中的配置文件
builder.Services.AddAutoMapper(typeof(UserProfile));

// 注册权限授权策略
builder.Services.AddPermissionAuthorization();

// 添加服务到容器
builder.Services.AddControllers(options =>
{
    // 全局注册 ValidateModelAttribute
    options.Filters.Add<ValidateModelAttribute>();
    options.Filters.Add<HttpResponseExceptionFilter>();
}).AddNewtonsoftJson(options =>
{
    // 可选：在此处配置 Newtonsoft.Json 的设置
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
});

var app = builder.Build();

// 执行数据初始化
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 调用数据初始化方法
        await DataSeeder.SeedRolesAndPermissionsAsync(services);
    }
    catch (Exception ex)
    {
        // 在控制台输出错误
        Console.WriteLine($"数据初始化失败：{ex.Message}");
    }
}

// 动态注册权限策略
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var authorizationOptions = services.GetRequiredService<IAuthorizationPolicyProvider>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    var permissions = dbContext.Permissions.ToList();

    //var authorizationOptionsMutable = options => { /* 这里需要扩展 */ };

    //foreach (var permission in permissions)
    //{
    //    options.AddPolicy(permission.Name, policy =>
    //        policy.Requirements.Add(new PermissionRequirement(permission.Name)));
    //}
}
app.UseCors("AllowSpecificOriginsWithCredentials");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
