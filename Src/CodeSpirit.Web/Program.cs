using Audit.Core;
using CodeSpirit.Amis;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Authorization;
using CodeSpirit.Authorization.Extensions;
using CodeSpirit.Charts.Extensions;
using CodeSpirit.Core;
using CodeSpirit.Messaging.Extensions;
using CodeSpirit.Messaging.Hubs;
// using CodeSpirit.MultiTenant.Extensions;
// using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.Navigation.Extensions;
using CodeSpirit.UdlCards.Extensions;
using CodeSpirit.ServiceDefaults;
using CodeSpirit.Shared.EventBus.Extensions;
using CodeSpirit.Shared.Extensions;
using CodeSpirit.Shared.Notifications.Events;
using CodeSpirit.Shared.Services;
using CodeSpirit.Shared.Services.Background;
using CodeSpirit.Shared.Services.Files;
using CodeSpirit.Web.Extensions;
using CodeSpirit.Web.Hubs;
using CodeSpirit.Web.Middlewares;
using CodeSpirit.Web.Options;
using CodeSpirit.Web.Services.EventHandlers;
using Microsoft.AspNetCore.Components;
using System.Text;

/// <summary>
/// Web前端应用程序入口点
/// </summary>
public class Program
{
    /// <summary>
    /// 应用程序主入口方法
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>异步任务</returns>
    public static async Task Main(string[] args)
    {
        // 设置控制台编码为UTF-8以正确显示中文字符
        Console.OutputEncoding = Encoding.UTF8;

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults("webfrontend");

        // Add messaging service client
        builder.Services.AddHttpClient("Messaging", client =>
        {
            client.BaseAddress = new Uri("http://messaging");
        });

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        // 注册站点配置选项
        builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("SiteSettings"));

        // Add HttpClient for Blazor components
        builder.Services.AddHttpClient();
        
        // 为租户登录页面配置专用的HttpClient
        builder.Services.AddHttpClient("TenantLogin", client =>
        {
            // 在运行时动态设置BaseAddress，因为我们在开发时使用相对路径
            // 生产环境中可以通过配置文件设置具体的API地址
        });

        // 添加 HttpContextAccessor 和内存缓存
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddCorsPolicy();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();

        // 添加多租户支持
        // builder.Services.AddCodeSpiritMultiTenant(builder.Configuration);

        // 使用共享项目中的JWT认证扩展方法
        builder.Services.AddJwtAuthentication(builder.Configuration);

        // 添加应用程序自定义服务
        builder.Services.AddApplicationServices();

        // 添加图表服务
        builder.Services.AddChartServices(options =>
        {
            options.EnableCache = true;
            options.CacheExpiration = 30; // 缓存30分钟
            options.DefaultProvider = "echarts";
        });

        // 添加 UDL Cards 服务
        builder.Services.AddUdlCards(options =>
        {
            options.DefaultTheme = "primary";
            options.EnablePermissionControl = true;
            options.StrictMode = false;
        });

        // 添加消息模块服务
        builder.Services.AddMessagingServices(builder.Configuration);
        builder.Services.AddRealtimeChat();
        builder.Services.AddCodeSpiritAuthorization();
        builder.Services.AddCodeSpiritNavigation();
        builder.Services.ConfigureDefaultControllers();
        // 添加代理相关服务，包括聚合器
        builder.Services.AddProxyServices();

        // 添加SignalR服务
        builder.Services.AddSignalR();

        // 注册事件总线服务
        builder.Services.AddEventBus();

        // 注册事件处理器
        builder.Services.AddEventHandler<SessionNotificationEvent, NotificationEventHandler>();

        // 注册后台任务服务
        builder.Services.AddHostedService<BackgroundJobServiceImpl>();
        builder.Services.AddSingleton<IBackgroundJobService>(provider =>
            provider.GetRequiredService<IEnumerable<IHostedService>>()
                .OfType<BackgroundJobServiceImpl>()
                .First());

        // 注册文件服务
        builder.Services.AddScoped<ITempFileService, TempFileServiceImpl>();

        builder.AddElasticsearchClient(connectionName: "elasticsearch", null, clientSettings =>
        {
            //临时禁用SSL验证，生产环境请务必配置正确的证书验证
            clientSettings.ServerCertificateValidationCallback((_, _, _, _) => true); // 禁用SSL验证
        });

        // 添加审计组件配置 - 使用原始扩展方法避免冲突
        AuditExtensions.AddAuditServices(builder.Services, builder.Configuration);

        //注册 AMIS 服务
        builder.Services.AddAmisServices(builder.Configuration, apiAssembly: typeof(Program).Assembly);

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

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapHub<ChatHub>("/chathub");
        app.MapHub<NotificationHub>("/notification-hub");

        app.UseCors("AllowSpecificOriginsWithCredentials");

        // 在认证之前添加租户路径解析中间件
        // app.UseMiddleware<TenantPathMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        await app.UseCodeSpiritNavigationAsync();
        app.MapControllers();

        // 在中间件管道中注册 (在应用程序的 Configure 方法中)
        app.UseAudit();

        // 确保在代理中间件之前注册
        app.UseMiddleware<ProxyMiddleware>();

        app.UseAmis();

        await app.RunAsync();
    }
}
