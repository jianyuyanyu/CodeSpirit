using CodeSpirit.Amis;
using CodeSpirit.ServiceDefaults;

Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults("identity-api");

// Add services to the container.
//builder.Services.AddProblemDetails();

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

////依赖注入驱动注册
//builder.Services.AddScopedRegister<IScopedDependency>();
//builder.Services.AddTransientRegister<ITransientDependency>();
//builder.Services.AddSingletonRegister<ISingletonDependency>();

WebApplication app = builder.Build();

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

app.UseCors("AllowSpecificOriginsWithCredentials");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
