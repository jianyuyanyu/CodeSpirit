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
    /// 添加标准API服务配置（简化版本）
    /// </summary>
    /// <remarks>
    /// 自动配置JWT、LLM、AI表单填充等通用配置
    /// </remarks>
    public static IResourceBuilder<ProjectResource> AddStandardApiService<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<IResourceWithConnectionString> database,
        AppParameters parameters,
        IResourceBuilder<IResourceWithConnectionString> cache,
        IResourceBuilder<RabbitMQServerResource> rabbitmqService,
        IResourceBuilder<ProjectResource>? identityService,
        string databaseType,
        IResourceBuilder<IResourceWithConnectionString>? settingsDb = null)
        where TProject : IProjectMetadata, new()
    {
        var service = builder.AddProject<TProject>(name)
            .WithReference(database)
            .WithReference(cache)
            .WithReference(rabbitmqService);

        // 只有当 identityService 不为 null 时才添加引用
        if (identityService != null)
        {
            service = service.WithReference(identityService);
        }

        service = service.WithEnvironment("DatabaseType", databaseType)
            // JWT配置
            .WithEnvironment("Jwt__SecretKey", parameters.Jwt.SecretKey)
            .WithEnvironment("Jwt__Issuer", parameters.Jwt.Issuer)
            .WithEnvironment("Jwt__Audience", parameters.Jwt.Audience)
            // LLM配置
            .WithEnvironment("LLM__ApiKey", parameters.Llm.ApiKey)
            .WithEnvironment("LLM__ApiBaseUrl", parameters.Llm.ApiBaseUrl)
            .WithEnvironment("LLM__ModelName", parameters.Llm.ModelName)
            .WithEnvironment("LLM__TimeoutSeconds", parameters.Llm.TimeoutSeconds)
            .WithEnvironment("LLM__MaxTokens", parameters.Llm.MaxTokens)
            .WithEnvironment("LLM__UseProxy", parameters.Llm.UseProxy)
            .WithEnvironment("LLM__ProxyAddress", parameters.Llm.ProxyAddress)
            // AI表单填充LLM配置
            .WithEnvironment("AiFormFillLLM__ApiKey", parameters.AiFormFillLlm.ApiKey)
            .WithEnvironment("AiFormFillLLM__ApiBaseUrl", parameters.AiFormFillLlm.ApiBaseUrl)
            .WithEnvironment("AiFormFillLLM__ModelName", parameters.AiFormFillLlm.ModelName)
            .WithEnvironment("AiFormFillLLM__DisableThinking", parameters.AiFormFillLlm.DisableThinking)
            .WithEnvironment("AiFormFillLLM__ResponseFormatType", parameters.AiFormFillLlm.ResponseFormatType)
            .WithEnvironment("AiFormFillLLM__Temperature", parameters.AiFormFillLlm.Temperature)
            .WithEnvironment("AiFormFillLLM__TopP", parameters.AiFormFillLlm.TopP)
            .WithEnvironment("AiFormFillLLM__EnableStreaming", parameters.AiFormFillLlm.EnableStreaming)
            .WaitFor(database);

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

