using CodeSpirit.Audit.Helpers;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Metrics;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计记录服务实现
/// </summary>
/// <remarks>
/// 专门负责审计日志的记录功能
/// </remarks>
public class AuditRecorder : IAuditRecorder
{
    private readonly IAuditStorageService _storageService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<AuditRecorder> _logger;
    private readonly AuditOptions _options;
    private readonly AuditMetrics _metrics;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditRecorder(
        IAuditStorageService storageService,
        IRabbitMQService rabbitMQService,
        ILogger<AuditRecorder> logger,
        IConfiguration configuration,
        AuditMetrics metrics)
    {
        _storageService = storageService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
        _options = ConfigurationHelper.BindAuditOptions(configuration);
        _metrics = metrics;
    }
    
    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <remarks>
    /// 降级策略：RabbitMQ（持久化） → 直接存储 → 记录错误日志
    /// 审计失败不阻塞业务，快速失败原则
    /// </remarks>
    public async Task RecordAsync(Models.AuditLog auditLog)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _metrics.IncrementTotalAuditLogs();
        
        try
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("审计功能已禁用，跳过记录");
                return;
            }
            
            _logger.LogDebug("开始记录审计日志: {Id}", auditLog.Id);
            
            try 
            {
                // 第一级：尝试推送到RabbitMQ（持久化队列）
                await _rabbitMQService.SendMessageAsync(auditLog);
                _metrics.IncrementRabbitMQSuccess();
                _metrics.IncrementSuccessfulAuditLogs();
                _logger.LogDebug("审计日志已推送到消息队列: {Id}", auditLog.Id);
                return; // 成功则返回
            }
            catch (Exception rabbitMQEx)
            {
                // RabbitMQ失败，降级到直接存储
                _metrics.IncrementRabbitMQFailure();
                _metrics.IncrementDegradation();
                _logger.LogWarning(rabbitMQEx, "RabbitMQ服务不可用，降级到直接存储: {Id}", auditLog.Id);
                
                try
                {
                    // 第二级：直接存储
                    await _storageService.StoreAsync(auditLog);
                    _metrics.IncrementStorageSuccess();
                    _metrics.IncrementSuccessfulAuditLogs();
                    _logger.LogDebug("审计日志已直接存储: {Id}", auditLog.Id);
                    return; // 成功则返回
                }
                catch (Exception storageEx)
                {
                    // 存储也失败，记录错误日志（不抛出异常，不影响业务）
                    _metrics.IncrementStorageFailure();
                    _metrics.IncrementCompleteFailure();
                    _metrics.IncrementFailedAuditLogs();
                    _logger.LogError(storageEx, "存储审计日志失败，审计数据将丢失: {Id}, UserId={UserId}, Operation={Operation}", 
                        auditLog.Id, auditLog.UserId, auditLog.OperationName);
                    // 不抛出异常，审计失败不应影响业务
                }
            }
        }
        catch (Exception ex)
        {
            // 捕获所有未预期的异常，记录日志但不抛出
            _metrics.IncrementFailedAuditLogs();
            _logger.LogError(ex, "记录审计日志时发生未预期错误: {Id}", auditLog.Id);
            // 不抛出异常，审计失败不应影响业务
        }
        finally
        {
            stopwatch.Stop();
            _metrics.AddProcessingTime(stopwatch.ElapsedMilliseconds);
        }
    }
}
