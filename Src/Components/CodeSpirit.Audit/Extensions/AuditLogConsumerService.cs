using Microsoft.Extensions.Hosting;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 审计日志消费者后台服务
/// </summary>
public class AuditLogConsumerService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<AuditLogConsumerService> _logger;
    private readonly AuditOptions _options;
    private string? _consumerTag;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(30);
    private readonly int _maxRetryAttempts = 10;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogConsumerService(
        IRabbitMQService rabbitMQService,
        IElasticsearchService elasticsearchService,
        IGeoLocationService geoLocationService,
        IConfiguration configuration,
        ILogger<AuditLogConsumerService> logger)
    {
        _rabbitMQService = rabbitMQService;
        _elasticsearchService = elasticsearchService;
        _geoLocationService = geoLocationService;
        _logger = logger;
        
        // 获取配置 - 智能处理配置绑定
        var options = new AuditOptions();
        if (configuration.GetSection("Audit").Exists())
        {
            // 传入的是完整配置，获取Audit节
            configuration.GetSection("Audit").Bind(options);
        }
        else
        {
            // 传入的就是Audit配置节
            configuration.Bind(options);
        }
        _options = options;
    }
    
    /// <summary>
    /// 执行服务
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("审计日志消费者服务正在启动...");
        
        int retryCount = 0;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 尝试初始化服务
                await InitializeServiceAsync(stoppingToken);
                
                _logger.LogInformation("审计日志消费者服务初始化成功，开始监听消息...");
                retryCount = 0; // 重置重试计数
                
                // 等待取消令牌或服务中断
                await WaitForCancellationAsync(stoppingToken);
                
                break; // 正常退出
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("审计日志消费者服务正在停止...");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogError(ex, "审计日志消费者初始化失败（第 {RetryCount}/{MaxRetryAttempts} 次尝试）", 
                    retryCount, _maxRetryAttempts);
                
                // 清理可能的部分初始化状态
                await CleanupAsync();
                
                if (retryCount >= _maxRetryAttempts)
                {
                    _logger.LogCritical("审计日志消费者服务重试次数已达上限，服务将停止");
                    break;
                }
                
                // 等待一段时间后重试
                try
                {
                    _logger.LogInformation("将在 {DelaySeconds} 秒后重试初始化", _retryDelay.TotalSeconds);
                    await Task.Delay(_retryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("服务停止信号已收到，取消重试");
                    break;
                }
            }
        }
        
        _logger.LogInformation("审计日志消费者服务已停止");
    }
    
    /// <summary>
    /// 初始化服务
    /// </summary>
    private async Task InitializeServiceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== 开始初始化审计日志消费者服务 ===");
        
        // 检查配置
        _logger.LogInformation("检查审计配置...");
        _logger.LogInformation("审计功能启用: {Enabled}", _options.Enabled);
        _logger.LogInformation("地理位置功能启用: {GeoEnabled}", _options.EnableGeoLocation);
        if (_options.RabbitMQ != null)
        {
            _logger.LogInformation("RabbitMQ配置 - 交换机: {Exchange}, 队列: {Queue}, 路由键: {RoutingKey}",
                _options.RabbitMQ.ExchangeName, _options.RabbitMQ.QueueName, _options.RabbitMQ.RoutingKey);
        }
        else
        {
            _logger.LogError("RabbitMQ配置为空！");
            throw new InvalidOperationException("RabbitMQ配置未找到");
        }
        
        if (_options.Elasticsearch != null)
        {
            _logger.LogInformation("Elasticsearch配置 - 索引: {Index}, URLs: {Urls}",
                _options.Elasticsearch.IndexName, string.Join(", ", _options.Elasticsearch.Urls));
        }
        else
        {
            _logger.LogError("Elasticsearch配置为空！");
            throw new InvalidOperationException("Elasticsearch配置未找到");
        }
        
        // 确保Elasticsearch索引存在
        _logger.LogInformation("正在检查Elasticsearch索引...");
        try
        {
            var indexCreated = await _elasticsearchService.CreateIndexAsync();
            _logger.LogInformation("Elasticsearch索引检查完成，结果: {Result}", indexCreated);
            
            if (!indexCreated)
            {
                _logger.LogWarning("Elasticsearch索引创建失败，但继续初始化消费者");
            }
        }
        catch (Exception esEx)
        {
            _logger.LogError(esEx, "检查Elasticsearch索引时发生异常: {Type}: {Message}", 
                esEx.GetType().Name, esEx.Message);
            throw;
        }
        
        // 订阅审计日志消息
        _logger.LogInformation("正在订阅RabbitMQ消息...");
        try
        {
            _consumerTag = _rabbitMQService.SubscribeMessage<Models.AuditLog>(ProcessAuditLogAsync);
            
            if (string.IsNullOrEmpty(_consumerTag))
            {
                throw new InvalidOperationException("RabbitMQ消息订阅失败，未返回消费者标签");
            }
            
            _logger.LogInformation("=== 审计日志消费者初始化成功 ===");
            _logger.LogInformation("消费者标签: {ConsumerTag}", _consumerTag);
        }
        catch (Exception mqEx)
        {
            _logger.LogError(mqEx, "订阅RabbitMQ消息时发生异常: {Type}: {Message}", 
                mqEx.GetType().Name, mqEx.Message);
            throw;
        }
    }
    
    /// <summary>
    /// 等待取消信号或监控服务状态
    /// </summary>
    private async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        // 创建定期健康检查任务
        var healthCheckTask = PerformHealthCheckAsync(cancellationToken);
        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
        
        // 等待取消信号或健康检查失败
        await Task.WhenAny(healthCheckTask, cancellationTask);
        
        if (healthCheckTask.IsCompletedSuccessfully)
        {
            var healthCheckResult = await healthCheckTask;
            if (!healthCheckResult)
            {
                throw new InvalidOperationException("服务健康检查失败，需要重新初始化");
            }
        }
    }
    
    /// <summary>
    /// 定期健康检查
    /// </summary>
    private async Task<bool> PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 等待5分钟后检查
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                
                // 检查消费者是否还在运行
                if (string.IsNullOrEmpty(_consumerTag))
                {
                    _logger.LogWarning("消费者标签丢失，服务可能已断开");
                    return false;
                }
                
                _logger.LogDebug("审计日志消费者健康检查正常");
            }
            catch (OperationCanceledException)
            {
                // 正常取消，退出循环
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "健康检查过程中发生异常");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 处理审计日志消息
    /// </summary>
    private async Task ProcessAuditLogAsync(Models.AuditLog auditLog)
    {
        var messageId = auditLog?.Id ?? "unknown";
        
        try
        {
            _logger.LogInformation("=== 开始处理审计日志消息 === ID: {Id}", messageId);
            
            if (auditLog == null)
            {
                _logger.LogError("收到空的审计日志消息");
                throw new ArgumentNullException(nameof(auditLog), "审计日志消息为空");
            }
            
            _logger.LogDebug("审计日志详情: UserId={UserId}, OperationType={OperationType}, Path={Path}", 
                auditLog.UserId, auditLog.OperationType, auditLog.RequestPath);
            
            // 处理地理位置
            try
            {
                _logger.LogDebug("开始处理地理位置信息...");
                await EnrichWithGeoLocationAsync(auditLog);
                _logger.LogDebug("地理位置信息处理完成");
            }
            catch (Exception geoEx)
            {
                _logger.LogError(geoEx, "处理地理位置信息失败 - ID: {Id}, 异常类型: {ExceptionType}, 消息: {Message}", 
                    messageId, geoEx.GetType().Name, geoEx.Message);
                
                if (geoEx.InnerException != null)
                {
                    _logger.LogError("地理位置服务内部异常: {InnerExceptionType}: {InnerMessage}", 
                        geoEx.InnerException.GetType().Name, geoEx.InnerException.Message);
                }
                
                // 地理位置失败不应该阻止整个处理流程，继续执行
            }
            
            // 保存到Elasticsearch
            try
            {
                _logger.LogDebug("开始保存到Elasticsearch...");
                await _elasticsearchService.IndexDocumentAsync(auditLog);
                _logger.LogInformation("=== 审计日志处理完成 === ID: {Id}", messageId);
            }
            catch (Exception esEx)
            {
                _logger.LogError(esEx, "保存到Elasticsearch失败 - ID: {Id}, 异常类型: {ExceptionType}, 消息: {Message}", 
                    messageId, esEx.GetType().Name, esEx.Message);
                
                if (esEx.InnerException != null)
                {
                    _logger.LogError("Elasticsearch服务内部异常: {InnerExceptionType}: {InnerMessage}", 
                        esEx.InnerException.GetType().Name, esEx.InnerException.Message);
                }
                
                // 记录详细的堆栈跟踪以便调试
                _logger.LogError("Elasticsearch异常堆栈跟踪: {StackTrace}", esEx.StackTrace);
                
                throw; // Elasticsearch失败必须重新抛出异常
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== 审计日志消费失败 === ID: {Id}, 异常类型: {ExceptionType}, 消息: {Message}", 
                messageId, ex.GetType().Name, ex.Message);
            
            // 记录详细的异常信息
            if (ex.InnerException != null)
            {
                _logger.LogError("内部异常: {InnerExceptionType}: {InnerMessage}", 
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }
            
            // 记录完整的堆栈跟踪用于调试
            _logger.LogError("完整异常堆栈跟踪: {StackTrace}", ex.StackTrace);
            
            throw; // 重新抛出异常以便RabbitMQ可以重新入队消息
        }
    }
    
    /// <summary>
    /// 为审计日志添加地理位置信息
    /// </summary>
    private async Task EnrichWithGeoLocationAsync(Models.AuditLog auditLog)
    {
        // 如果未启用地理位置或IP地址为空，则跳过
        if (!_options.EnableGeoLocation || string.IsNullOrEmpty(auditLog.IpAddress))
        {
            return;
        }
        
        try
        {
            // 获取地理位置信息
            var geoLocation = await _geoLocationService.GetLocationByIpAsync(auditLog.IpAddress);
            if (geoLocation != null)
            {
                auditLog.Location = geoLocation;
                _logger.LogDebug("已为审计日志 {Id} 添加地理位置信息: {Country}, {City}", 
                    auditLog.Id, geoLocation.Country, geoLocation.City);
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不中断流程
            _logger.LogWarning(ex, "获取IP地址 {IpAddress} 的地理位置信息时发生错误", auditLog.IpAddress);
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    private async Task CleanupAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                _rabbitMQService.Unsubscribe(_consumerTag);
                _consumerTag = null;
                _logger.LogDebug("已清理RabbitMQ订阅");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理资源时发生异常");
        }
        
        // 添加短暂延迟确保清理完成
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// 停止服务
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("审计日志消费者正在停止...");
        
        await CleanupAsync();
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("审计日志消费者已完全停止");
    }
} 