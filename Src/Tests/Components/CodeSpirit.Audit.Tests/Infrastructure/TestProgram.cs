using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 测试程序入口类，用于WebApplicationFactory（仅作为Marker Class）
/// </summary>
public class TestProgram
{
    // 这个类只是作为WebApplicationFactory的标记类，不需要任何方法
}

/// <summary>
/// 测试启动类，用于WebApplicationFactory
/// </summary>
public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => 
        {
            endpoints.MapControllers();
        });
    }
} 