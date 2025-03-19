// Program.cs
using System.Text;
using CodeSpirit.ExamApi;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddExam();

var app = builder.Build();

try
{
    await app.UseExamApiServicesAsync();
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "应用程序启动过程中发生错误");
    Console.WriteLine($"应用程序启动失败: {ex.Message}");
}