using CodeSpirit.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
//builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorPages();

// Add WebOptimizer
builder.Services.AddWebOptimizer(pipeline =>
{
    // Minify and bundle JavaScript files
    pipeline.MinifyJsFiles("/sdk/sdk.js", "/js/*.js");

    // Minify CSS files
    pipeline.MinifyCssFiles("/sdk/antd.css", "/sdk/helper.css", "/sdk/iconfont.css", "/css/*.css");
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

//app.UseOutputCache();

app.MapRazorPages();

app.MapDefaultEndpoints();

// Add WebOptimizer middleware
app.UseWebOptimizer();

app.Run();
