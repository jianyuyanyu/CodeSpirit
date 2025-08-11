using CodeSpirit.FileStorageApi;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddFileStorage();

var app = builder.Build();

try
{
    await app.UseFileStorageApiServicesAsync();    
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "文件存储服务启动过程中发生错误");
    Console.WriteLine($"文件存储服务启动失败: {ex.Message}");
}
