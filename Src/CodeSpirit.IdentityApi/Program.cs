Console.OutputEncoding = System.Text.Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddIdentityApiServices();

WebApplication app = builder.Build();
await app.ConfigureAppAsync();

app.Run();
