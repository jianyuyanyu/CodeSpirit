using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ��� SignalR
builder.Services.AddSignalR();

// ע�����û������
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConfigCacheService, MemoryCacheConfigService>();

// ע�����ñ��֪ͨ����
builder.Services.AddSingleton<IConfigChangeNotifier, SignalRConfigChangeNotifier>();

// ... ��������ע�� ...

// ���� SignalR �ս��
//app.MapHub<ConfigChangeHub>("/hubs/configChange");

////�ͻ�������ʾ��
//// ���� SignalR ����
//const connection = new signalR.HubConnectionBuilder()
//.withUrl("/hubs/configChange")
//    .build();

//// �������ñ��
//connection.on("ConfigChanged", (appId, environment) => {
//console.log(`Config changed for ${ appId}/${ environment}`);
//// ���¼�������
//loadConfigs();
//});

//// ���ӵ� Hub
//await connection.start();

//// ����Ӧ��������
//await connection.invoke("JoinAppGroup", appId, environment);

var app = builder.Build();
app.Run();
