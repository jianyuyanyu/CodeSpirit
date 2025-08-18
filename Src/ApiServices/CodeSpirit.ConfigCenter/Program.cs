using CodeSpirit.ConfigCenter.Configuration;
using CodeSpirit.Shared.Startup;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 使用统一的API启动框架
builder.AddCodeSpiritApi<ConfigCenterApiConfiguration>();

var app = builder.Build();

try
{
    // 使用统一的API配置
    await app.UseCodeSpiritApiAsync<ConfigCenterApiConfiguration>();
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "配置中心服务启动过程中发生错误");
    Console.WriteLine($"配置中心服务启动失败: {ex.Message}");
}
