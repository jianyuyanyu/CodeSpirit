using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 使用 MessagingApi 扩展方法注册所有服务
builder.AddMessagingApi();

WebApplication app = builder.Build();

// 配置中间件
await app.ConfigureAppAsync();
app.Run();
