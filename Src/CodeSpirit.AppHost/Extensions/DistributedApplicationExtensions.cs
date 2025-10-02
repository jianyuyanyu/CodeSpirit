using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CodeSpirit.AppHost.Extensions;

/// <summary>
/// AppHost 扩展方法集合
/// </summary>
public static class DistributedApplicationExtensions
{
    /// <summary>
    /// 添加通用JWT配置
    /// </summary>
    public static IResourceBuilder<T> WithJwtConfiguration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> secretKey,
        IResourceBuilder<ParameterResource> issuer,
        IResourceBuilder<ParameterResource> audience) 
        where T : IResourceWithEnvironment
    {
        return builder
            .WithEnvironment("Jwt__SecretKey", secretKey)
            .WithEnvironment("Jwt__Issuer", issuer)
            .WithEnvironment("Jwt__Audience", audience);
    }

    /// <summary>
    /// 添加通用LLM配置
    /// </summary>
    public static IResourceBuilder<T> WithLlmConfiguration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> apiKey,
        IResourceBuilder<ParameterResource> apiBaseUrl,
        IResourceBuilder<ParameterResource> modelName,
        IResourceBuilder<ParameterResource> timeoutSeconds,
        IResourceBuilder<ParameterResource> maxTokens,
        IResourceBuilder<ParameterResource> useProxy,
        IResourceBuilder<ParameterResource> proxyAddress) 
        where T : IResourceWithEnvironment
    {
        return builder
            .WithEnvironment("LLM__ApiKey", apiKey)
            .WithEnvironment("LLM__ApiBaseUrl", apiBaseUrl)
            .WithEnvironment("LLM__ModelName", modelName)
            .WithEnvironment("LLM__TimeoutSeconds", timeoutSeconds)
            .WithEnvironment("LLM__MaxTokens", maxTokens)
            .WithEnvironment("LLM__UseProxy", useProxy)
            .WithEnvironment("LLM__ProxyAddress", proxyAddress);
    }

    /// <summary>
    /// 添加AI表单填充LLM配置
    /// </summary>
    public static IResourceBuilder<T> WithAiFormFillLlmConfiguration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource> apiKey,
        IResourceBuilder<ParameterResource> apiBaseUrl,
        IResourceBuilder<ParameterResource> modelName,
        IResourceBuilder<ParameterResource> disableThinking,
        IResourceBuilder<ParameterResource> responseFormatType,
        IResourceBuilder<ParameterResource> temperature,
        IResourceBuilder<ParameterResource> topP,
        IResourceBuilder<ParameterResource> enableStreaming) 
        where T : IResourceWithEnvironment
    {
        return builder
            .WithEnvironment("AiFormFillLLM__ApiKey", apiKey)
            .WithEnvironment("AiFormFillLLM__ApiBaseUrl", apiBaseUrl)
            .WithEnvironment("AiFormFillLLM__ModelName", modelName)
            .WithEnvironment("AiFormFillLLM__DisableThinking", disableThinking)
            .WithEnvironment("AiFormFillLLM__ResponseFormatType", responseFormatType)
            .WithEnvironment("AiFormFillLLM__Temperature", temperature)
            .WithEnvironment("AiFormFillLLM__TopP", topP)
            .WithEnvironment("AiFormFillLLM__EnableStreaming", enableStreaming);
    }

    /// <summary>
    /// 添加标准API服务依赖和配置
    /// </summary>
    public static IResourceBuilder<T> WithStandardApiConfiguration<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<IResourceWithConnectionString> database,
        IResourceBuilder<IResourceWithConnectionString> cache,
        IResourceBuilder<ProjectResource> seqService,
        IResourceBuilder<ProjectResource> configService,
        IResourceBuilder<RabbitMQServerResource> rabbitmqService,
        IResourceBuilder<ProjectResource> identityService,
        string databaseType,
        IResourceBuilder<ParameterResource> jwtSecretKey,
        IResourceBuilder<ParameterResource> jwtIssuer,
        IResourceBuilder<ParameterResource> jwtAudience,
        IResourceBuilder<ParameterResource> llmApiKey,
        IResourceBuilder<ParameterResource> llmApiBaseUrl,
        IResourceBuilder<ParameterResource> llmModelName,
        IResourceBuilder<ParameterResource> llmTimeoutSeconds,
        IResourceBuilder<ParameterResource> llmMaxTokens,
        IResourceBuilder<ParameterResource> llmUseProxy,
        IResourceBuilder<ParameterResource> llmProxyAddress,
        IResourceBuilder<ParameterResource> aiFormFillLlmApiKey,
        IResourceBuilder<ParameterResource> aiFormFillLlmApiBaseUrl,
        IResourceBuilder<ParameterResource> aiFormFillLlmModelName,
        IResourceBuilder<ParameterResource> aiFormFillLlmDisableThinking,
        IResourceBuilder<ParameterResource> aiFormFillLlmResponseFormatType,
        IResourceBuilder<ParameterResource> aiFormFillLlmTemperature,
        IResourceBuilder<ParameterResource> aiFormFillLlmTopP,
        IResourceBuilder<ParameterResource> aiFormFillLlmEnableStreaming) 
        where T : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        return builder
            .WithReference(database)
            .WithReference(seqService)
            .WithReference(cache)
            .WithReference(configService)
            .WithReference(rabbitmqService)
            .WithReference(identityService)
            .WithEnvironment("DatabaseType", databaseType)
            .WithJwtConfiguration(jwtSecretKey, jwtIssuer, jwtAudience)
            .WithLlmConfiguration(llmApiKey, llmApiBaseUrl, llmModelName, 
                llmTimeoutSeconds, llmMaxTokens, llmUseProxy, llmProxyAddress)
            .WithAiFormFillLlmConfiguration(aiFormFillLlmApiKey, aiFormFillLlmApiBaseUrl,
                aiFormFillLlmModelName, aiFormFillLlmDisableThinking, aiFormFillLlmResponseFormatType,
                aiFormFillLlmTemperature, aiFormFillLlmTopP, aiFormFillLlmEnableStreaming)
            .WaitFor(database);
    }
}

