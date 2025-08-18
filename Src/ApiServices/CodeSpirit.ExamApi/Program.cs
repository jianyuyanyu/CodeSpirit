using CodeSpirit.ExamApi.Configuration;
using CodeSpirit.Shared.Startup;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 使用统一的API启动框架
builder.AddCodeSpiritApi<ExamApiConfiguration>();

var app = builder.Build();

try
{
    // 使用统一的API配置
    await app.UseCodeSpiritApiAsync<ExamApiConfiguration>();
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "考试系统服务启动过程中发生错误");
    Console.WriteLine($"考试系统服务启动失败: {ex.Message}");
}