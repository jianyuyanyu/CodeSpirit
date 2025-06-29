using CodeSpirit.UdlCards.Builders;
using CodeSpirit.UdlCards.Core;
using CodeSpirit.UdlCards.Models;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.UdlCards.Extensions;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加UDL Cards服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUdlCards(this IServiceCollection services)
    {
        return services.AddUdlCards(options => { });
    }

    /// <summary>
    /// 添加UDL Cards服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUdlCards(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册配置选项
        services.Configure<UdlCardsOptions>(configuration.GetSection(UdlCardsOptions.SectionName));

        return RegisterServices(services);
    }

    /// <summary>
    /// 添加UDL Cards服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUdlCards(this IServiceCollection services, Action<UdlCardsOptions> configureOptions)
    {
        // 注册配置选项
        services.Configure(configureOptions);

        return RegisterServices(services);
    }

    /// <summary>
    /// 添加自定义卡片建构器
    /// </summary>
    /// <typeparam name="TConfig">卡片配置类型</typeparam>
    /// <typeparam name="TBuilder">建构器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddUdlCardBuilder<TConfig, TBuilder>(this IServiceCollection services)
        where TConfig : UdlCardConfig
        where TBuilder : class, IUdlCardBuilder<TConfig>
    {
        services.AddSingleton<IUdlCardBuilder<TConfig>, TBuilder>();
        return services;
    }

    /// <summary>
    /// 注册核心服务和建构器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // 避免重复注册
        if (services.Any(x => x.ServiceType == typeof(UdlCardsGenerator)))
        {
            return services;
        }

        // 注册核心服务
        services.AddSingleton<UdlCardsGenerator>();

        // 注册卡片建构器
        services.AddSingleton<IUdlCardBuilder<StatCardConfig>, StatCardBuilder>();
        services.AddSingleton<IUdlCardBuilder<ChartCardConfig>, ChartCardBuilder>();
        services.AddSingleton<IUdlCardBuilder<TableCardConfig>, TableCardBuilder>();
        services.AddSingleton<IUdlCardBuilder<InfoCardConfig>, InfoCardBuilder>();
        services.AddSingleton<IUdlCardBuilder<InfoGridCardConfig>, InfoGridCardBuilder>();

        // 注册基础接口（用于UdlCardsGenerator）
        services.AddSingleton<IUdlCardBuilderBase, StatCardBuilder>();
        services.AddSingleton<IUdlCardBuilderBase, ChartCardBuilder>();
        services.AddSingleton<IUdlCardBuilderBase, TableCardBuilder>();
        services.AddSingleton<IUdlCardBuilderBase, InfoCardBuilder>();
        services.AddSingleton<IUdlCardBuilderBase, InfoGridCardBuilder>();

        return services;
    }
}
 