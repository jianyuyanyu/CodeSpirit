Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddIdentityApiServices();

var app = builder.Build();
await app.InitializeDatabaseAsync();
app.ConfigureApp();

app.Run();
