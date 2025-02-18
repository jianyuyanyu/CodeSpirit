Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 使用 ConfigCenter 扩展方法注册所有服务
builder.AddConfigCenter();

WebApplication app = builder.Build();

// 配置中间件
app.UseCors("AllowSpecificOriginsWithCredentials");
app.ConfigureAppAsync();
app.Run();
