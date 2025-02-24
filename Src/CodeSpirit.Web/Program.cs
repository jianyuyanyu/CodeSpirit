using CodeSpirit.Core;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Web.Middlewares;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults("webfrontend");
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

        // 添加 HttpContextAccessor 和内存缓存
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddCorsPolicy();

        builder.Services.AddCodeSpiritNavigation();
        builder.Services.ConfigureDefaultControllers();

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

        app.UseCors("AllowSpecificOriginsWithCredentials");
        app.MapDefaultEndpoints();
        await app.UseCodeSpiritNavigationAsync();
        app.MapControllers();

        // Add WebOptimizer middleware
        app.UseWebOptimizer();
        app.UseMiddleware<ProxyMiddleware>();
        
        await app.RunAsync();
    }
}
