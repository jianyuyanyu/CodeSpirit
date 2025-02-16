using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 添加 SignalR
builder.Services.AddSignalR();

// 注册配置缓存服务
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConfigCacheService, MemoryCacheConfigService>();

// 注册配置变更通知服务
builder.Services.AddSingleton<IConfigChangeNotifier, SignalRConfigChangeNotifier>();

// ... 其他服务注册 ...

// 配置 SignalR 终结点
//app.MapHub<ConfigChangeHub>("/hubs/configChange");

////客户端连接示例
//// 创建 SignalR 连接
//const connection = new signalR.HubConnectionBuilder()
//.withUrl("/hubs/configChange")
//    .build();

//// 订阅配置变更
//connection.on("ConfigChanged", (appId, environment) => {
//console.log(`Config changed for ${ appId}/${ environment}`);
//// 重新加载配置
//loadConfigs();
//});

//// 连接到 Hub
//await connection.start();

//// 加入应用配置组
//await connection.invoke("JoinAppGroup", appId, environment);

var app = builder.Build();
app.Run();
