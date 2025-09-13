using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.MultiTenant.Services;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace CodeSpirit.MultiTenant.Extensions;

/// <summary>
/// 多租户服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 缓存的Identity服务检测结果
    /// </summary>
    private static bool? _isIdentityServiceCache;
    /// <summary>
    /// 添加多租户支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCodeSpiritMultiTenant(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册配置选项
        services.Configure<TenantOptions>(configuration.GetSection(TenantOptions.SectionName));

        // 获取配置选项
        var tenantOptions = configuration.GetSection(TenantOptions.SectionName).Get<TenantOptions>()
                           ?? new TenantOptions();

        // 注册核心服务
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<ITenantContext, TenantContext>();


        // 注册租户存储实现
        RegisterTenantStore(services, configuration);

        // 注册多租户数据过滤器
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(IMultiTenant)] = new DataFilterState(isEnabled: true);
        });

        return services;
    }

    /// <summary>
    /// 注册租户存储实现
    /// 根据当前服务类型自动选择合适的存储方式
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    private static void RegisterTenantStore(IServiceCollection services, IConfiguration configuration)
    {
        // 检测当前是否为Identity服务
        var isIdentityService = IsIdentityService();

        if (isIdentityService)
        {
            // Identity服务使用本地存储，直接访问数据库
            services.Configure<LocalTenantStoreOptions>(configuration.GetSection(LocalTenantStoreOptions.SectionName));
            services.AddScoped<ITenantStore, LocalTenantStore>();

            Console.WriteLine("检测到Identity服务，使用LocalTenantStore直接访问数据库");
        }
        else
        {
            // 其他服务使用统一租户存储（内存→分布式缓存→API的顺序）

            // 注册API租户存储配置
            services.Configure<ApiTenantStoreOptions>(configuration.GetSection(ApiTenantStoreOptions.SectionName));

            // 获取配置选项用于HttpClient配置
            var apiStoreOptions = configuration.GetSection(ApiTenantStoreOptions.SectionName).Get<ApiTenantStoreOptions>()
                                ?? new ApiTenantStoreOptions();

            // 注册命名HttpClient并配置
            services.AddHttpClient("ApiTenantStore", client =>
            {
                if (!string.IsNullOrEmpty(apiStoreOptions.BaseUrl))
                {
                    client.BaseAddress = new Uri(apiStoreOptions.BaseUrl);
                }

                if (apiStoreOptions.Timeout > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(apiStoreOptions.Timeout);
                }

                // 设置默认请求头
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "CodeSpirit.MultiTenant/1.0");
            });

            // 注册统一租户存储
            services.AddScoped<ITenantStore, UnifiedTenantStore>();

            Console.WriteLine("检测到非Identity服务，使用UnifiedTenantStore通过API访问");
        }
    }

    /// <summary>
    /// 检测当前是否为Identity服务
    /// 使用静态缓存提高性能，避免重复检测
    /// </summary>
    /// <returns>是否为Identity服务</returns>
    private static bool IsIdentityService()
    {
        // 如果已经缓存了结果，直接返回
        if (_isIdentityServiceCache.HasValue)
        {
            return _isIdentityServiceCache.Value;
        }

        try
        {
            // 检查程序集名称
            var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            if (!string.IsNullOrEmpty(assemblyName) &&
                assemblyName.Equals("CodeSpirit.IdentityApi", StringComparison.OrdinalIgnoreCase))
            {
                _isIdentityServiceCache = true;
                return true;
            }

            // 方法2: 检查当前目录路径
            var currentDirectory = Directory.GetCurrentDirectory();
            if (currentDirectory.Contains("IdentityApi", StringComparison.OrdinalIgnoreCase))
            {
                _isIdentityServiceCache = true;
                return true;
            }

            // 缓存false结果
            _isIdentityServiceCache = false;
            return false;
        }
        catch (Exception)
        {
            // 如果检测失败，默认返回false，使用API方式，并缓存结果
            _isIdentityServiceCache = false;
            return false;
        }
    }

    /// <summary>
    /// 添加多租户数据库上下文
    /// </summary>
    /// <typeparam name="TContext">数据库上下文类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="optionsAction">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMultiTenantDbContext<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var tenantResolver = serviceProvider.GetService<ITenantResolver>();
            if (tenantResolver != null)
            {
                var tenantId = tenantResolver.ResolveTenantIdAsync().GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(tenantId))
                {
                    var tenantInfo = tenantResolver.GetTenantInfoAsync(tenantId).GetAwaiter().GetResult();
                    if (tenantInfo != null)
                    {
                        // 根据租户策略配置数据库连接
                        switch (tenantInfo.Strategy)
                        {
                            case TenantStrategy.SeparateDatabase:
                                if (!string.IsNullOrEmpty(tenantInfo.ConnectionString))
                                {
                                    options.UseSqlServer(tenantInfo.ConnectionString);
                                    return;
                                }
                                break;
                            case TenantStrategy.SharedDatabaseSeparateSchema:
                                if (!string.IsNullOrEmpty(tenantInfo.ConnectionString))
                                {
                                    options.UseSqlServer(tenantInfo.ConnectionString);
                                    // 这里可以设置表前缀等配置
                                    return;
                                }
                                break;
                        }
                    }
                }
            }

            optionsAction(options);
        });

        return services;
    }

}