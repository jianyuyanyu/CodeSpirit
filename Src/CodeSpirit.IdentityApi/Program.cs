using CodeSpirit.ConfigCenter.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddIdentityApiServices();

// ���������������
builder.Host.ConfigureConfigCenterConfiguration((context, options) =>
{
    // �ӱ��������ж�ȡ��������ѡ��
    context.Configuration.GetSection("ConfigCenter").Bind(options);

    // Ҳ����ֱ������
    options.ServiceUrl = "https://config";
    options.AppId = "identity";
    options.AppSecret = "your-app-secret";
    options.Environment = context.HostingEnvironment.EnvironmentName;
    options.AutoRegisterApp = true;
    options.AppName = "�û�����";
});

// ����������Ŀͻ���
builder.Services.AddConfigCenterClient(options =>
{
    // �������ж�ȡ
    builder.Configuration.GetSection("ConfigCenter").Bind(options);
});

WebApplication app = builder.Build();
await app.ConfigureAppAsync();

// �����������Ŀͻ���
app.UseConfigCenterClient();

app.Run();
