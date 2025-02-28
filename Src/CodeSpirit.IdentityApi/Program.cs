using CodeSpirit.ConfigCenter.Client;
using Microsoft.AspNetCore.HttpLogging;
using System.Net.Http;

Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddIdentityApiServices();

// ���������������
//builder.Host.ConfigureConfigCenterConfiguration((context, options) =>
//{
//    // �ӱ��������ж�ȡ��������ѡ��
//    context.Configuration.GetSection("ConfigCenter").Bind(options);

//    // Ҳ����ֱ������
//    options.ServiceUrl = "http://config";
//    options.AppId = "identity";
//    options.AppSecret = "your-app-secret";
//    options.Environment = context.HostingEnvironment.EnvironmentName;
//    options.AutoRegisterApp = true;
//    options.AppName = "�û�����";
//});

// �� Program.cs ������������־
builder.Services.AddHttpLogging(logging =>
    logging.LoggingFields = HttpLoggingFields.All);

//// ����������Ŀͻ���
//builder.Services.AddConfigCenterClient(options =>
//{
//    // �������ж�ȡ
//    builder.Configuration.GetSection("ConfigCenter").Bind(options);
//});

builder.Services.AddHttpClient("config", client =>
    client.BaseAddress = new Uri("http://config"))
    .AddServiceDiscovery();

WebApplication app = builder.Build();
await app.ConfigureAppAsync();

// �����������Ŀͻ���
//app.UseConfigCenterClient();

var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var client = httpClientFactory.CreateClient("config");
var response = await client.GetAsync("/api/config/client/config/identity/Development");
string content = await response.Content.ReadAsStringAsync();
Console.WriteLine(content);
app.Run();
