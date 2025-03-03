using CodeSpirit.ConfigCenter.Client;
using Microsoft.AspNetCore.HttpLogging;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddIdentityApiServices();

// 添加配置中心服务（配置源和客户端）
builder.AddConfigCenter();

// 在 Program.cs 中启用请求日志
builder.Services.AddHttpLogging(logging =>
    logging.LoggingFields = HttpLoggingFields.All);

WebApplication app = builder.Build();

await app.ConfigureAppAsync();

// 启用配置中心客户端
app.UseConfigCenterClient();
app.Run();
