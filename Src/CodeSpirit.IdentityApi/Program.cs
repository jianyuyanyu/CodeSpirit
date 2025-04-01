using System.Text;
using CodeSpirit.IdentityApi;
using CodeSpirit.IdentityApi.Jwt;
using CodeSpirit.IdentityApi.Services;

Console.OutputEncoding = Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddIdentityApiServices();

// 注册JWT服务
builder.Services.AddScoped<IJwtTokenHandler, JwtTokenHandler>();

// 注册登录日志仓储
builder.Services.AddScoped<ILoginLogRepository, LoginLogRepository>();

WebApplication app = builder.Build();

await app.ConfigureAppAsync();
app.Run();
