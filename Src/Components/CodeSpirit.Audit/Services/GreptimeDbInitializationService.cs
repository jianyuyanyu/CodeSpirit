using CodeSpirit.Audit.Services.Implementation;
using CodeSpirit.Audit.Services.LLM;
using CodeSpirit.Audit.Services.LLM.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// GreptimeDB初始化服务
/// 在应用启动时主动初始化GreptimeDB数据库和表（包括普通审计和LLM审计）
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
            
            // 初始化普通审计存储服务
            await InitializeAuditStorageAsync(scope);
            
            // 初始化LLM审计存储服务
            await InitializeLLMAuditStorageAsync(scope);
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
    
    /// <summary>
    /// 初始化普通审计存储服务
    /// </summary>
    private async Task InitializeAuditStorageAsync(IServiceScope scope)
    {
        var auditStorageService = scope.ServiceProvider.GetService<IAuditStorageService>();

        if (auditStorageService is GreptimeDbAuditStorageService greptimeDbService)
        {
            _logger.LogInformation("检测到GreptimeDB审计存储服务，开始初始化");
            
            var initSuccess = await greptimeDbService.InitializeAsync();
            
            if (initSuccess)
            {
                _logger.LogInformation("GreptimeDB审计存储初始化成功");
                
                // 进行健康检查确认
                var isHealthy = await greptimeDbService.HealthCheckAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("GreptimeDB审计存储健康检查通过，服务就绪");
                }
                else
                {
                    _logger.LogWarning("GreptimeDB审计存储初始化成功但健康检查未通过，可能存在潜在问题");
                }
            }
            else
            {
                _logger.LogError("GreptimeDB审计存储初始化失败，审计功能可能无法正常工作");
            }
        }
        else
        {
            _logger.LogDebug("当前审计存储服务不是GreptimeDB，跳过普通审计存储初始化");
        }
    }
    
    /// <summary>
    /// 初始化LLM审计存储服务
    /// </summary>
    private async Task InitializeLLMAuditStorageAsync(IServiceScope scope)
    {
        var llmAuditStorageService = scope.ServiceProvider.GetService<ILLMAuditStorageService>();

        if (llmAuditStorageService is LLMGreptimeDbStorageService llmGreptimeDbService)
        {
            _logger.LogInformation("检测到LLM GreptimeDB审计存储服务，开始初始化");
            
            var initSuccess = await llmGreptimeDbService.InitializeAsync();
            
            if (initSuccess)
            {
                _logger.LogInformation("LLM GreptimeDB审计存储初始化成功");
                
                // 进行健康检查确认
                var isHealthy = await llmGreptimeDbService.HealthCheckAsync();
                if (isHealthy)
                {
                    _logger.LogInformation("LLM GreptimeDB审计存储健康检查通过，服务就绪");
                }
                else
                {
                    _logger.LogWarning("LLM GreptimeDB审计存储初始化成功但健康检查未通过，可能存在潜在问题");
                }
            }
            else
            {
                _logger.LogError("LLM GreptimeDB审计存储初始化失败，LLM审计功能可能无法正常工作");
            }
        }
        else
        {
            _logger.LogDebug("当前LLM审计存储服务不是GreptimeDB，跳过LLM审计存储初始化");
        }
    }
}
