using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace CodeSpirit.AppHost.Configuration;

/// <summary>
/// 应用参数管理类，集中管理所有配置参数
/// </summary>
/// <remarks>
/// 💡 JWT、LLM、AiFormFillLLM 等业务配置已迁移到配置中心种子数据，
/// 此处仅保留基础设施相关的敏感参数（数据库密码、RabbitMQ 凭据等）。
/// </remarks>
public class AppParameters
{
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
            Database = DatabaseParameters.Create(builder),
            RabbitMq = RabbitMqParameters.Create(builder)
        };
    }
}

// 💡 JWT、LLM、AiFormFillLLM 参数类已移除
// 这些配置已迁移到配置中心种子数据，服务启动后通过配置中心 SDK 自动获取

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

