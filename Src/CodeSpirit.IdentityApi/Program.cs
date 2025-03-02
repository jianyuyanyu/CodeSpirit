using CodeSpirit.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;

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
app.Run();
