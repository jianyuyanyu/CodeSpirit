using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

try
{
    // Add service defaults & Aspire client integrations
    builder.AddServiceDefaults("messaging");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not add service defaults: {ex.Message}");
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add messaging services
builder.Services.AddMessagingServices(builder.Configuration);
builder.Services.AddRealtimeChat();

// Configure EF Core to ignore pending model changes warning
builder.Services.AddDbContext<CodeSpirit.Messaging.Data.MessagingDbContext>(options => 
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("messaging-api"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());
    
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
}, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CodeSpirit.Messaging.Data.MessagingDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("数据库迁移应用成功！");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"应用迁移时发生错误: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

try
{
    app.MapDefaultEndpoints();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not map default endpoints: {ex.Message}");
}

app.Run();
