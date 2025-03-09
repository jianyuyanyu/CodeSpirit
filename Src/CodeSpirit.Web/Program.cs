using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Web.Extensions;
using CodeSpirit.Web.Middlewares;
using Microsoft.AspNetCore.Components;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults("webfrontend");
        //builder.AddRedisOutputCache("cache");

        // Add messaging service client
        builder.Services.AddHttpClient("Messaging", client =>
        {
            client.BaseAddress = new Uri("http://messaging");
        });

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        // Add HttpClient for Blazor components
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<HttpClient>(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
            return httpClient;
        });

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
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        
        // 添加应用程序自定义服务
        builder.Services.AddApplicationServices();

        // 添加消息模块服务
        builder.Services.AddMessagingServices(builder.Configuration);
        builder.Services.AddRealtimeChat();

        builder.Services.AddCodeSpiritNavigation();
        builder.Services.ConfigureDefaultControllers();
        // 添加代理相关服务，包括聚合器
        builder.Services.AddProxyServices();

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
        app.MapBlazorHub();
        app.MapHub<ChatHub>("/chathub");

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
