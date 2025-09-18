using CodeSpirit.Audit.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// GreptimeDB初始化服务
/// 在应用启动时主动初始化GreptimeDB数据库和表
/// </summary>
public class GreptimeDbInitializationService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<GreptimeDbInitializationService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public GreptimeDbInitializationService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GreptimeDbInitializationService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始GreptimeDB初始化服务");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditStorageService = scope.ServiceProvider.GetService<IAuditStorageService>();

            if (auditStorageService is GreptimeDbAuditStorageService greptimeDbService)
            {
                _logger.LogInformation("检测到GreptimeDB审计存储服务，开始初始化");
                
                var initSuccess = await greptimeDbService.InitializeAsync();
                
                if (initSuccess)
                {
                    _logger.LogInformation("GreptimeDB初始化成功");
                    
                    // 进行健康检查确认
                    var isHealthy = await greptimeDbService.HealthCheckAsync();
                    if (isHealthy)
                    {
                        _logger.LogInformation("GreptimeDB健康检查通过，服务就绪");
                    }
                    else
                    {
                        _logger.LogWarning("GreptimeDB初始化成功但健康检查未通过，可能存在潜在问题");
                    }
                }
                else
                {
                    _logger.LogError("GreptimeDB初始化失败，审计功能可能无法正常工作");
                }
            }
            else
            {
                _logger.LogDebug("当前审计存储服务不是GreptimeDB，跳过初始化");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GreptimeDB初始化服务启动失败");
            // 不抛出异常，避免影响应用启动
        }

        _logger.LogInformation("GreptimeDB初始化服务启动完成");
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GreptimeDB初始化服务停止");
        return Task.CompletedTask;
    }
}
