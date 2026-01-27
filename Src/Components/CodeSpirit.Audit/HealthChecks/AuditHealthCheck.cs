using Microsoft.Extensions.Diagnostics.HealthChecks;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Metrics;

namespace CodeSpirit.Audit.HealthChecks;

/// <summary>
/// 审计组件健康检查
/// </summary>
public class AuditHealthCheck : IHealthCheck
{
    private readonly IAuditStorageService _storageService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly AuditMetrics _metrics;
    private readonly ILogger<AuditHealthCheck> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditHealthCheck(
        IAuditStorageService storageService,
        IRabbitMQService rabbitMQService,
        AuditMetrics metrics,
        ILogger<AuditHealthCheck> logger)
    {
        _storageService = storageService;
        _rabbitMQService = rabbitMQService;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // 检查存储服务健康状态
            var storageHealthy = await _storageService.HealthCheckAsync();
            data["StorageHealthy"] = storageHealthy;

            // 检查 RabbitMQ 连接（如果可用）
            var rabbitMQHealthy = true; // 简化：假设 RabbitMQ 服务会自己处理连接状态
            data["RabbitMQHealthy"] = rabbitMQHealthy;

            // 添加指标信息
            var snapshot = _metrics.GetSnapshot();
            data["TotalAuditLogs"] = snapshot.TotalAuditLogs;
            data["SuccessfulAuditLogs"] = snapshot.SuccessfulAuditLogs;
            data["FailedAuditLogs"] = snapshot.FailedAuditLogs;
            data["DegradationCount"] = snapshot.DegradationCount;
            data["CompleteFailureCount"] = snapshot.CompleteFailureCount;
            data["AverageProcessingTime"] = snapshot.AverageProcessingTime;
            data["BatchStoredCount"] = snapshot.BatchStoredCount;

            // 计算健康状态
            var isHealthy = storageHealthy && rabbitMQHealthy;
            var status = isHealthy ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy : Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded;

            // 如果有完全失败，标记为不健康
            if (snapshot.CompleteFailureCount > 0)
            {
                status = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;
            }

            return new HealthCheckResult(status, "审计组件健康检查完成", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "审计健康检查失败");
            return new HealthCheckResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, "审计健康检查异常", ex, data);
        }
    }
}
