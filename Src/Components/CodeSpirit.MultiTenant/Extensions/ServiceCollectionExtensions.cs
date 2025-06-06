using CodeSpirit.Core;
using CodeSpirit.MultiTenant.Abstractions;
using CodeSpirit.MultiTenant.Models;
using CodeSpirit.MultiTenant.Services;
using CodeSpirit.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.MultiTenant.Extensions;

/// <summary>
/// 多租户服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
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

        // 注册核心服务
        services.AddScoped<ITenantResolver, TenantResolver>();
        
        // 根据配置选择租户存储实现
        var tenantOptions = configuration.GetSection(TenantOptions.SectionName).Get<TenantOptions>() 
                           ?? new TenantOptions();

        switch (tenantOptions.StoreType)
        {
            case TenantStoreType.Memory:
                services.AddSingleton<ITenantStore, MemoryTenantStore>();
                break;
            case TenantStoreType.ConfigFile:
                // TODO: 实现配置文件存储
                services.AddSingleton<ITenantStore, MemoryTenantStore>();
                break;
            case TenantStoreType.Api:
                // 注册API租户存储配置
                services.Configure<ApiTenantStoreOptions>(configuration.GetSection(ApiTenantStoreOptions.SectionName));
                // 注册HttpClient
                services.AddHttpClient<ApiTenantStore>();
                // 注册API租户存储
                services.AddScoped<ITenantStore, ApiTenantStore>();
                break;
            case TenantStoreType.Database:
            default:
                // TODO: 实现数据库存储
                services.AddSingleton<ITenantStore, MemoryTenantStore>();
                break;
        }

        // 注册多租户数据过滤器
        services.Configure<DataFilterOptions>(options =>
        {
            options.DefaultStates[typeof(IMultiTenant)] = new DataFilterState(isEnabled: true);
        });

        return services;
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