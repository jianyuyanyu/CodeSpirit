using Microsoft.Extensions.Configuration;
using CodeSpirit.Audit.Models;

namespace CodeSpirit.Audit.Helpers;

/// <summary>
/// 配置绑定助手
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// 智能绑定审计配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>审计配置选项</returns>
    public static AuditOptions BindAuditOptions(IConfiguration configuration)
    {
        var options = new AuditOptions();
        
        if (configuration.GetSection("Audit").Exists())
        {
            // 传入的是完整配置，获取Audit节
            configuration.GetSection("Audit").Bind(options);
        }
        else
        {
            // 传入的就是Audit配置节
            configuration.Bind(options);
        }
        
        return options;
    }
    
    /// <summary>
    /// 智能绑定Elasticsearch配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>Elasticsearch配置选项</returns>
    public static ElasticsearchOptions BindElasticsearchOptions(IConfiguration configuration)
    {
        var auditOptions = BindAuditOptions(configuration);
        return auditOptions.Elasticsearch;
    }
    
    /// <summary>
    /// 智能绑定RabbitMQ配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <returns>RabbitMQ配置选项</returns>
    public static RabbitMQOptions BindRabbitMQOptions(IConfiguration configuration)
    {
        var auditOptions = BindAuditOptions(configuration);
        return auditOptions.RabbitMQ;
    }
} 