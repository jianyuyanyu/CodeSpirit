using CodeSpirit.Amis;
using CodeSpirit.IdentityApi.Authorization;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Authorization;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
//builder.Services.AddProblemDetails();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddDataFilters();
builder.Services.AddCustomServices();
builder.Services.AddIdentityServices();
builder.Services.AddCorsPolicy();
// 如果需要启用 JWT 认证，取消注释以下行
// builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddFluentValidationServices();
builder.Services.ConfigureControllers();
builder.Services.AddAmisServices(builder.Configuration, apiAssembly: typeof(Program).Assembly);

////依赖注入驱动注册
//builder.Services.AddScopedRegister<IScopedDependency>();
//builder.Services.AddTransientRegister<ITransientDependency>();
//builder.Services.AddSingletonRegister<ISingletonDependency>();

// 注册权限授权策略
builder.Services.AddPermissionAuthorization();
var app = builder.Build();

// 执行数据初始化
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<SeederService>>();
    try
    {
        // 调用数据初始化方法
        await DataSeeder.SeedAsync(services, logger);
    }
    catch (Exception ex)
    {
        // 在控制台输出错误
        logger.LogError(ex, $"数据初始化失败：{ex.Message}");
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
