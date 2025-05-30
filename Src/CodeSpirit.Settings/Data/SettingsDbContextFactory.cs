using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CodeSpirit.Settings.Data;

/// <summary>
/// SettingsDbContext 设计时工厂
/// </summary>
public class SettingsDbContextFactory : IDesignTimeDbContextFactory<SettingsDbContext>
{
    /// <summary>
    /// 创建DbContext实例
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>SettingsDbContext实例</returns>
    public SettingsDbContext CreateDbContext(string[] args)
    {
        // 构建配置
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true);

        var configuration = configBuilder.Build();

        // 创建服务集合
        var services = new ServiceCollection();

        // 添加基础服务
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUser, DesignTimeCurrentUser>();
        services.AddSingleton<IHostEnvironment, DesignTimeHostEnvironment>();
        
        // 添加日志服务
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // 构建服务提供者
        var serviceProvider = services.BuildServiceProvider();

        // 验证配置是否正确
        var testConfig = serviceProvider.GetRequiredService<IConfiguration>();
        var multiTenantSection = testConfig.GetSection("MultiTenant");
        if (!multiTenantSection.Exists())
        {
            // 如果配置不存在，添加默认配置
            var dict = new Dictionary<string, string?>
            {
                ["MultiTenant:Enabled"] = "true",
                ["MultiTenant:DefaultTenantId"] = "default"
            };
            
            configBuilder.AddInMemoryCollection(dict);
            configuration = configBuilder.Build();
            
            // 重新创建服务提供者
            services.Clear();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ICurrentUser, DesignTimeCurrentUser>();
            services.AddSingleton<IHostEnvironment, DesignTimeHostEnvironment>();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            serviceProvider = services.BuildServiceProvider();
        }

        // 配置DbContext选项
        var optionsBuilder = new DbContextOptionsBuilder<SettingsDbContext>();
        var connectionString = configuration.GetConnectionString("settings") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=codespirit-settings;Trusted_Connection=True;MultipleActiveResultSets=true;Packet Size=512";
        
        optionsBuilder.UseSqlServer(connectionString);

        return new SettingsDbContext(
            optionsBuilder.Options, 
            serviceProvider, 
            serviceProvider.GetRequiredService<ICurrentUser>(),
            serviceProvider.GetRequiredService<IHttpContextAccessor>());
    }
}

/// <summary>
/// 设计时当前用户实现
/// </summary>
internal class DesignTimeCurrentUser : ICurrentUser
{
    public long? Id => 1; // 设计时使用固定用户ID
    public string? UserName => "design-time-user";
    public string? Name => "Design Time User";
    public string? TenantId => "default"; // 设计时使用默认租户
    public string? TenantName => "Default Tenant";
    public bool IsAuthenticated => true;
    public string[] Roles => new[] { "Admin" };
    public HashSet<string> Permissions => new HashSet<string>();
    public IEnumerable<Claim> Claims => new List<Claim>();

    public bool IsInRole(string role) => Roles.Contains(role);
    public bool IsInTenant(string tenantId) => TenantId == tenantId;
}

/// <summary>
/// 设计时主机环境实现
/// </summary>
internal class DesignTimeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "CodeSpirit.Settings";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
} 