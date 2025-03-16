using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;
using System.IO;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 集成测试基类
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly ITestOutputHelper _output;
    protected readonly HttpClient _client;
    protected readonly WebApplicationFactory<TestProgram> _factory;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        _output = output;
        _factory = new TestWebApplicationFactory(this);
        _client = _factory.CreateClient();
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
        
        _output.WriteLine("测试初始化完成");
    }
    
    // 配置服务方法，供派生类重写
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        _output.WriteLine("配置服务");
    }
    
    // 配置中间件方法，供派生类重写
    protected virtual void ConfigureAuditMiddleware(IApplicationBuilder app)
    {
        _output.WriteLine("配置中间件");
    }
    
    // 获取注册的服务
    protected T GetService<T>() where T : class
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }
    
    // 内部WebApplicationFactory类，用于自定义服务配置
    protected class TestWebApplicationFactory : WebApplicationFactory<TestProgram>
    {
        private readonly IntegrationTestBase _testBase;
        
        public TestWebApplicationFactory(IntegrationTestBase testBase)
        {
            _testBase = testBase;
        }
        
        protected override IHostBuilder CreateHostBuilder()
        {
            // 获取当前项目路径
            var projectDir = Directory.GetCurrentDirectory();
            
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(projectDir);
                    webBuilder.UseStartup<TestStartup>();
                });
        }
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // 获取当前项目路径
            var projectDir = Directory.GetCurrentDirectory();
            
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(projectDir);
            
            builder.ConfigureServices(services => 
            {
                _testBase.ConfigureServices(services);
            });
            
            builder.Configure(app => 
            {
                // 配置中间件
                _testBase.ConfigureAuditMiddleware(app);
                
                // 注册路由
                app.UseRouting();
                
                // 注册终结点
                app.UseEndpoints(endpoints => 
                {
                    endpoints.MapControllers();
                });
            });
        }
    }
} 