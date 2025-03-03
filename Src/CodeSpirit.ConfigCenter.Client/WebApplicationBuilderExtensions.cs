using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.ConfigCenter.Client;

/// <summary>
/// WebApplicationBuilder的配置中心扩展方法
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// 添加配置中心服务（同时配置为配置源和注册客户端服务）
    /// </summary>
    /// <param name="builder">Web应用构建器</param>
    /// <param name="configSectionName">配置节名称，默认为"ConfigCenter"</param>
    /// <returns>Web应用构建器</returns>
    public static WebApplicationBuilder AddConfigCenter(
        this WebApplicationBuilder builder, 
        string configSectionName = "ConfigCenter")
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // 获取配置选项
        var configSection = builder.Configuration.GetSection(configSectionName);
        
        // 配置为配置源
        builder.Host.ConfigureConfigCenterConfiguration((context, options) => 
        {
            context.Configuration.GetSection(configSectionName).Bind(options);
        });
        
        // 注册客户端服务
        builder.Services.AddConfigCenterClient(options => 
        {
            configSection.Bind(options);
        });
        
        return builder;
    }
} 