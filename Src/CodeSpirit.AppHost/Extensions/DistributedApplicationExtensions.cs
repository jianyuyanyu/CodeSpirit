using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CodeSpirit.AppHost.Extensions;

/// <summary>
/// AppHost 扩展方法集合
/// </summary>
/// <remarks>
/// 💡 JWT、LLM、AiFormFillLLM 等配置相关的扩展方法已移除。
/// 这些配置已迁移到配置中心种子数据，服务启动后通过配置中心 SDK 自动获取。
/// </remarks>
public static class DistributedApplicationExtensions
{
    /// <summary>
    /// 添加标准API服务依赖和配置（简化版本）
    /// </summary>
    /// <remarks>
    /// 配置数据库、缓存、消息队列等基础设施引用。
    /// JWT、LLM、AiFormFillLLM 等业务配置由配置中心 SDK 自动获取。
    /// </remarks>
    public static IResourceBuilder<T> WithStandardApiConfiguration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithConnectionString> database,
        IResourceBuilder<IResourceWithConnectionString> cache,
        IResourceBuilder<ProjectResource> seqService,
        IResourceBuilder<ProjectResource> configService,
        IResourceBuilder<RabbitMQServerResource> rabbitmqService,
        IResourceBuilder<ProjectResource> identityService,
        string databaseType) 
        where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        return builder
            .WithReference(database)
            .WithReference(seqService)
            .WithReference(cache)
            .WaitFor(cache)  // ⚠️ 重要：等待 Redis 完全启动
            .WithReference(configService)
            .WithReference(rabbitmqService)
            .WithReference(identityService)
            .WithEnvironment("DatabaseType", databaseType)
            // 💡 JWT、LLM、AiFormFillLLM 等配置由配置中心 SDK 自动获取
            .WaitFor(database);
    }
}
