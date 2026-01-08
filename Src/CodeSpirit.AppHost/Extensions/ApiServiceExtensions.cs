using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CodeSpirit.AppHost.Configuration;

namespace CodeSpirit.AppHost.Extensions;

/// <summary>
/// API服务注册扩展方法
/// </summary>
public static class ApiServiceExtensions
{
    /// <summary>
    /// 添加标准API服务配置
    /// </summary>
    /// <remarks>
    /// 自动配置：数据库、缓存、消息队列、日志、配置中心、健康检查等。
    /// 💡 JWT、LLM、AiFormFillLLM、Audit 等业务配置已迁移到配置中心种子数据，
    /// 服务启动后通过配置中心 SDK 自动获取。
    /// </remarks>
    /// <param name="builder">分布式应用构建器</param>
    /// <param name="name">服务名称</param>
    /// <param name="database">主数据库</param>
    /// <param name="parameters">应用参数</param>
    /// <param name="cache">Redis 缓存</param>
    /// <param name="rabbitmqService">RabbitMQ 消息队列</param>
    /// <param name="seqService">Seq 日志服务</param>
    /// <param name="configService">配置中心服务（可选，ConfigCenter 自身不需要）</param>
    /// <param name="identityService">身份认证服务（可选）</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="version">服务版本号</param>
    /// <param name="settingsDb">设置数据库（可选）</param>
    public static IResourceBuilder<ProjectResource> AddStandardApiService<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<IResourceWithConnectionString> database,
        AppParameters parameters,
        IResourceBuilder<IResourceWithConnectionString> cache,
        IResourceBuilder<RabbitMQServerResource> rabbitmqService,
        IResourceBuilder<SeqResource> seqService,
        IResourceBuilder<ProjectResource>? configService,
        IResourceBuilder<ProjectResource>? identityService,
        string databaseType,
        string version = "1.0.0",
        IResourceBuilder<IResourceWithConnectionString>? settingsDb = null)
        where TProject : IProjectMetadata, new()
    {
        var service = builder.AddProject<TProject>(name)
            .WithReference(database)
            .WithReference(cache)
            .WaitFor(cache)  // ⚠️ 重要：等待 Redis 完全启动
            .WithReference(rabbitmqService)
            .WithReference(seqService);  // Seq 日志服务

        // 配置中心引用（ConfigCenter 自身不需要）
        if (configService != null)
        {
            service = service.WithReference(configService);
        }

        // 身份认证服务引用
        if (identityService != null)
        {
            service = service.WithReference(identityService);
        }

        service = service.WithEnvironment("ServiceName", name)
            .WithEnvironment("DatabaseType", databaseType)
            .WaitFor(database)
            .WithHealthCheck()  // 健康检查
            .WithEnvironmentAwareDeploymentTag(name, () => version);  // 部署标签

        // 如果需要访问设置数据库
        if (settingsDb != null)
        {
            service = service.WithReference(settingsDb).WaitFor(settingsDb);
        }

        return service;
    }

    /// <summary>
    /// 添加健康检查端点配置
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithHealthCheck(
        this IResourceBuilder<ProjectResource> builder,
        string endpoint = "https",
        string healthPath = "/health")
    {
        return builder.WithUrlForEndpoint(endpoint, ep => new()
        {
            Url = healthPath,
            DisplayText = "健康检查",
            DisplayLocation = UrlDisplayLocation.DetailsOnly
        });
    }

    /// <summary>
    /// 添加部署环境感知的镜像标签
    /// </summary>
    /// <remarks>
    /// 注意：WithDeploymentImageTag 是 Aspire 9.5 的实验性功能，目前已被注释
    /// 此方法保留用于将来启用该功能时使用
    /// </remarks>
    public static IResourceBuilder<ProjectResource> WithEnvironmentAwareDeploymentTag(
        this IResourceBuilder<ProjectResource> builder,
        string serviceName,
        Func<string>? versionProvider = null)
    {
        // 实验性功能 WithDeploymentImageTag 当前已被注释
        // 如需启用，请取消下面代码的注释
        /*
        return builder.WithDeploymentImageTag(_ =>
        {
            var version = versionProvider?.Invoke() ?? GetDefaultVersion();
            var gitCommit = GetGitCommitHash();
            var shortCommit = gitCommit.Length >= 8 ? gitCommit[..8] : gitCommit;
            return $"{serviceName}-{version}-{shortCommit}";
        });
        */
        
        // 暂时返回 builder 本身（空操作）
        return builder;
    }

    /// <summary>
    /// 获取默认版本号
    /// </summary>
    private static string GetDefaultVersion()
    {
        // 可以从 AssemblyInfo 或环境变量获取
        return Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0.0";
    }

    /// <summary>
    /// 获取Git提交哈希
    /// </summary>
    private static string GetGitCommitHash()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return "unknown";
        }
    }
}

