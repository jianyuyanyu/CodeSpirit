// Program.cs
using CodeSpirit.ExamApi;
using CodeSpirit.ExamApi.Extensions;
using System.Text;
using CodeSpirit.Shared.DistributedLock;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddExam();

// 添加AI题目生成服务 - 使用Extensions命名空间下的方法
CodeSpirit.ExamApi.Extensions.DependencyInjectionExtensions.AddAIQuestionGeneratorServices(builder.Services);

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