using Microsoft.Extensions.Options;

namespace CodeSpirit.Audit.Models;

/// <summary>
/// 审计选项配置验证器
/// </summary>
public class AuditOptionsValidator : IValidateOptions<AuditOptions>
{
    /// <summary>
    /// 验证配置选项
    /// </summary>
    /// <param name="name">选项名称</param>
    /// <param name="options">选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, AuditOptions options)
    {
        var errors = new List<string>();

        // 验证存储提供者
        if (string.IsNullOrWhiteSpace(options.StorageProvider))
        {
            errors.Add("StorageProvider 不能为空");
        }
        else if (!options.StorageProvider.Equals("Elasticsearch", StringComparison.OrdinalIgnoreCase) &&
                 !options.StorageProvider.Equals("GreptimeDB", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"StorageProvider 必须是 'Elasticsearch' 或 'GreptimeDB'，当前值: {options.StorageProvider}");
        }

        // 验证 Elasticsearch 配置
        if (options.StorageProvider.Equals("Elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            if (options.Elasticsearch == null)
            {
                errors.Add("Elasticsearch 配置不能为空");
            }
            else
            {
                if (options.Elasticsearch.Urls == null || !options.Elasticsearch.Urls.Any())
                {
                    errors.Add("Elasticsearch:Urls 不能为空");
                }

                if (string.IsNullOrWhiteSpace(options.Elasticsearch.IndexName))
                {
                    errors.Add("Elasticsearch:IndexName 不能为空");
                }
            }
        }

        // 验证 GreptimeDB 配置
        if (options.StorageProvider.Equals("GreptimeDB", StringComparison.OrdinalIgnoreCase))
        {
            if (options.GreptimeDB == null)
            {
                errors.Add("GreptimeDB 配置不能为空");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.GreptimeDB.Url))
                {
                    errors.Add("GreptimeDB:Url 不能为空");
                }

                if (string.IsNullOrWhiteSpace(options.GreptimeDB.Database))
                {
                    errors.Add("GreptimeDB:Database 不能为空");
                }

                if (string.IsNullOrWhiteSpace(options.GreptimeDB.TableName))
                {
                    errors.Add("GreptimeDB:TableName 不能为空");
                }

                // 验证 URL 格式
                if (!string.IsNullOrWhiteSpace(options.GreptimeDB.Url) &&
                    !Uri.TryCreate(options.GreptimeDB.Url, UriKind.Absolute, out _))
                {
                    errors.Add($"GreptimeDB:Url 格式无效: {options.GreptimeDB.Url}");
                }
            }
        }

        // 验证 RabbitMQ 配置（如果启用）
        if (options.RabbitMQ != null)
        {
            if (string.IsNullOrWhiteSpace(options.RabbitMQ.ExchangeName))
            {
                errors.Add("RabbitMQ:ExchangeName 不能为空");
            }

            if (string.IsNullOrWhiteSpace(options.RabbitMQ.QueueName))
            {
                errors.Add("RabbitMQ:QueueName 不能为空");
            }
        }

        if (errors.Any())
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
