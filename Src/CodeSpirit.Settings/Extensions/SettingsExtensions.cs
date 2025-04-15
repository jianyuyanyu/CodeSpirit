using CodeSpirit.Settings.Data;
using CodeSpirit.Settings.Services.Implementations;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.Settings.Extensions;

/// <summary>
/// 设置扩展方法
/// </summary>
public static class SettingsExtensions
{
    /// <summary>
    /// 添加设置管理服务（包含数据库）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="dbContextOptions">数据库上下文选项配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSettingsManagerWithDatabase(
        this IServiceCollection services, 
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> dbContextOptions = null)
    {
        // 添加数据库上下文
        if (dbContextOptions != null)
        {
            services.AddDbContext<SettingsDbContext>(dbContextOptions);
        }
        else
        {
            // 默认使用SQL Server
            var connectionString = configuration.GetConnectionString("settings");
            services.AddDbContext<SettingsDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        
        // 注册设置服务
        services.AddScoped<ISettingsService, SettingsService>();
        
        return services;
    }
    
    /// <summary>
    /// 初始化设置数据库
    /// </summary>
    /// <param name="app">应用程序</param>
    /// <returns>应用程序</returns>
    public static async Task<IApplicationBuilder> UseSettingsManagerAsync(this IApplicationBuilder app)
    {
        // 获取服务范围
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var services = scope.ServiceProvider;
            
            try
            {
                // 获取数据库上下文
                var context = services.GetRequiredService<SettingsDbContext>();
                
                // 应用迁移
                await context.Database.MigrateAsync();
                
                // 初始化数据
                // 检查是否有设置数据
                if (!await context.SettingItems.AnyAsync())
                {
                    // 可以在这里添加初始化设置数据的逻辑
                    // 例如从配置文件加载预设设置等
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<SettingsDbContext>>();
                logger.LogError(ex, "初始化设置数据库时出错");
            }
        }
        
        return app;
    }
    
    /// <summary>
    /// 获取强类型设置值
    /// </summary>
    public static T GetValue<T>(this Dictionary<string, string> settings, string key, T defaultValue = default)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// 获取布尔值设置
    /// </summary>
    public static bool GetBool(this Dictionary<string, string> settings, string key, bool defaultValue = false)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
    
    /// <summary>
    /// 获取整数设置
    /// </summary>
    public static int GetInt(this Dictionary<string, string> settings, string key, int defaultValue = 0)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
    
    /// <summary>
    /// 获取小数设置
    /// </summary>
    public static decimal GetDecimal(this Dictionary<string, string> settings, string key, decimal defaultValue = 0)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }
    
    /// <summary>
    /// 获取JSON反序列化对象
    /// </summary>
    public static T GetJson<T>(this Dictionary<string, string> settings, string key, T defaultValue = default)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return defaultValue;
        }
    }
} 