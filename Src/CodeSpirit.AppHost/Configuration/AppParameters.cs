using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CodeSpirit.AppHost.Configuration;

/// <summary>
/// 应用参数管理类，集中管理所有配置参数
/// </summary>
public class AppParameters
{
    /// <summary>
    /// JWT配置参数
    /// </summary>
    public JwtParameters Jwt { get; init; } = null!;

    /// <summary>
    /// LLM配置参数
    /// </summary>
    public LlmParameters Llm { get; init; } = null!;

    /// <summary>
    /// AI表单填充LLM配置参数
    /// </summary>
    public AiFormFillLlmParameters AiFormFillLlm { get; init; } = null!;

    /// <summary>
    /// 数据库配置参数
    /// </summary>
    public DatabaseParameters Database { get; init; } = null!;

    /// <summary>
    /// RabbitMQ配置参数
    /// </summary>
    public RabbitMqParameters RabbitMq { get; init; } = null!;

    /// <summary>
    /// 从应用构建器创建参数实例
    /// </summary>
    public static AppParameters Create(IDistributedApplicationBuilder builder)
    {
        return new AppParameters
        {
            Jwt = JwtParameters.Create(builder),
            Llm = LlmParameters.Create(builder),
            AiFormFillLlm = AiFormFillLlmParameters.Create(builder),
            Database = DatabaseParameters.Create(builder),
            RabbitMq = RabbitMqParameters.Create(builder)
        };
    }
}

/// <summary>
/// JWT配置参数
/// </summary>
public class JwtParameters
{
    public IResourceBuilder<ParameterResource> SecretKey { get; init; } = null!;
    public IResourceBuilder<ParameterResource> Issuer { get; init; } = null!;
    public IResourceBuilder<ParameterResource> Audience { get; init; } = null!;

    public static JwtParameters Create(IDistributedApplicationBuilder builder)
    {
        return new JwtParameters
        {
            SecretKey = builder.AddParameter("jwt-SecretKey", "ECBF8FA013844D77AE041A6800D7FF8F", secret: true),
            Issuer = builder.AddParameter("jwt-Issuer", "codespirit.com"),
            Audience = builder.AddParameter("jwt-Audience", "CodeSpirit")
        };
    }
}

/// <summary>
/// LLM配置参数
/// </summary>
public class LlmParameters
{
    public IResourceBuilder<ParameterResource> ApiKey { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ApiBaseUrl { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ModelName { get; init; } = null!;
    public IResourceBuilder<ParameterResource> TimeoutSeconds { get; init; } = null!;
    public IResourceBuilder<ParameterResource> MaxTokens { get; init; } = null!;
    public IResourceBuilder<ParameterResource> UseProxy { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ProxyAddress { get; init; } = null!;

    public static LlmParameters Create(IDistributedApplicationBuilder builder)
    {
        return new LlmParameters
        {
            ApiKey = builder.AddParameter("llm-ApiKey", secret: true),
            ApiBaseUrl = builder.AddParameter("llm-ApiBaseUrl", "https://dashscope.aliyuncs.com/compatible-mode/v1"),
            ModelName = builder.AddParameter("llm-ModelName", "qwen-plus"),
            TimeoutSeconds = builder.AddParameter("llm-TimeoutSeconds", "120"),
            MaxTokens = builder.AddParameter("llm-MaxTokens", "2048"),
            UseProxy = builder.AddParameter("llm-UseProxy", "false"),
            ProxyAddress = builder.AddParameter("llm-ProxyAddress", "", secret: false)
        };
    }
}

/// <summary>
/// AI表单填充LLM配置参数
/// </summary>
public class AiFormFillLlmParameters
{
    public IResourceBuilder<ParameterResource> ApiKey { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ApiBaseUrl { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ModelName { get; init; } = null!;
    public IResourceBuilder<ParameterResource> DisableThinking { get; init; } = null!;
    public IResourceBuilder<ParameterResource> ResponseFormatType { get; init; } = null!;
    public IResourceBuilder<ParameterResource> Temperature { get; init; } = null!;
    public IResourceBuilder<ParameterResource> TopP { get; init; } = null!;
    public IResourceBuilder<ParameterResource> EnableStreaming { get; init; } = null!;

    public static AiFormFillLlmParameters Create(IDistributedApplicationBuilder builder)
    {
        return new AiFormFillLlmParameters
        {
            ApiKey = builder.AddParameter("ai-form-fill-llm-ApiKey", secret: true),
            ApiBaseUrl = builder.AddParameter("ai-form-fill-llm-ApiBaseUrl", "https://dashscope.aliyuncs.com/compatible-mode/v1"),
            ModelName = builder.AddParameter("ai-form-fill-llm-ModelName", "qwen-flash"),
            DisableThinking = builder.AddParameter("ai-form-fill-llm-DisableThinking", "true"),
            ResponseFormatType = builder.AddParameter("ai-form-fill-llm-ResponseFormatType", "json_object"),
            Temperature = builder.AddParameter("ai-form-fill-llm-Temperature", "0.1"),
            TopP = builder.AddParameter("ai-form-fill-llm-TopP", "0.9"),
            EnableStreaming = builder.AddParameter("ai-form-fill-llm-EnableStreaming", "true")
        };
    }
}

/// <summary>
/// 数据库配置参数
/// </summary>
public class DatabaseParameters
{
    public IResourceBuilder<ParameterResource>? MySqlPassword { get; init; }
    public IResourceBuilder<ParameterResource>? SqlServerPassword { get; init; }

    public static DatabaseParameters Create(IDistributedApplicationBuilder builder)
    {
        return new DatabaseParameters
        {
            MySqlPassword = builder.AddParameter("mysql-password", "Password123", secret: true),
            SqlServerPassword = builder.AddParameter("sqlserver-password", "P@ssword123456", secret: true)
        };
    }
}

/// <summary>
/// RabbitMQ配置参数
/// </summary>
public class RabbitMqParameters
{
    public IResourceBuilder<ParameterResource> Username { get; init; } = null!;
    public IResourceBuilder<ParameterResource> Password { get; init; } = null!;

    public static RabbitMqParameters Create(IDistributedApplicationBuilder builder)
    {
        return new RabbitMqParameters
        {
            Username = builder.AddParameter("rabbitmq-username", "admin"),
            Password = builder.AddParameter("rabbitmq-password", "Password123", secret: true)
        };
    }
}

