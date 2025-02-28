using CodeSpirit.ConfigCenter.Client;
using Microsoft.AspNetCore.HttpLogging;
using System.Net.Http;

Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddIdentityApiServices();

// 添加配置中心配置
//builder.Host.ConfigureConfigCenterConfiguration((context, options) =>
//{
//    // 从本地配置中读取配置中心选项
//    context.Configuration.GetSection("ConfigCenter").Bind(options);

//    // 也可以直接设置
//    options.ServiceUrl = "http://config";
//    options.AppId = "identity";
//    options.AppSecret = "your-app-secret";
//    options.Environment = context.HostingEnvironment.EnvironmentName;
//    options.AutoRegisterApp = true;
//    options.AppName = "用户中心";
//});

// 在 Program.cs 中启用请求日志
builder.Services.AddHttpLogging(logging =>
    logging.LoggingFields = HttpLoggingFields.All);

//// 添加配置中心客户端
//builder.Services.AddConfigCenterClient(options =>
//{
//    // 从配置中读取
//    builder.Configuration.GetSection("ConfigCenter").Bind(options);
//});

builder.Services.AddHttpClient("config", client =>
    client.BaseAddress = new Uri("http://config"))
    .AddServiceDiscovery();

WebApplication app = builder.Build();
await app.ConfigureAppAsync();

// 启用配置中心客户端
//app.UseConfigCenterClient();

var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var client = httpClientFactory.CreateClient("config");
var response = await client.GetAsync("/api/config/client/config/identity/Development");
string content = await response.Content.ReadAsStringAsync();
Console.WriteLine(content);
app.Run();
