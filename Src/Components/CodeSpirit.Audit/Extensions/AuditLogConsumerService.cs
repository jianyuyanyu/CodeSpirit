using Microsoft.Extensions.Hosting;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace CodeSpirit.Audit.Extensions;

/// <summary>
/// 审计日志消费者后台服务
/// </summary>
public class AuditLogConsumerService : BackgroundService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<AuditLogConsumerService> _logger;
    private readonly AuditOptions _options;
    private readonly AuditMetrics _metrics;
    private string? _consumerTag;
    private readonly TimeSpan _maxRetryDelay = TimeSpan.FromSeconds(60); // 最大重试间隔
    
    // 批量处理相关
    private readonly List<Models.AuditLog> _batchBuffer = new();
    private readonly object _batchLock = new object();
    private Timer? _flushTimer;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogConsumerService(
        IRabbitMQService rabbitMQService,
        IServiceScopeFactory serviceScopeFactory,
        IGeoLocationService geoLocationService,
        IConfiguration configuration,
        ILogger<AuditLogConsumerService> logger,
        AuditMetrics metrics)
    {
        _rabbitMQService = rabbitMQService;
        _serviceScopeFactory = serviceScopeFactory;
        _geoLocationService = geoLocationService;
        _logger = logger;
        _metrics = metrics;
        
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
        
        // 初始化批量处理配置
        _batchSize = _options.RabbitMQ?.BatchSize ?? 100;
        _flushInterval = TimeSpan.FromSeconds(_options.RabbitMQ?.BatchFlushIntervalSeconds ?? 5);
    }
    
    /// <summary>
    /// 启动时初始化定时刷新
    /// </summary>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // 启动定时刷新任务
        _flushTimer = new Timer(async _ => await FlushBatchAsync(), null, _flushInterval, _flushInterval);
        return base.StartAsync(cancellationToken);
    }
    
    /// <summary>
    /// 停止时清理资源
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("审计日志消费者正在停止...");
        
        // 停止定时刷新
        _flushTimer?.Dispose();
        
        // 停止前刷新剩余的批次
        try
        {
            await FlushBatchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "停止时刷新批量缓冲失败");
        }
        
        // 清理RabbitMQ订阅
        await CleanupAsync();
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("审计日志消费者已完全停止");
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
                
                // 计算指数退避延迟（1s, 2s, 4s, 8s...最大60s）
                var delaySeconds = Math.Min(Math.Pow(2, retryCount - 1), _maxRetryDelay.TotalSeconds);
                var delay = TimeSpan.FromSeconds(delaySeconds);
                
                _logger.LogWarning(ex, "审计日志消费者初始化失败（第 {RetryCount} 次尝试），将在 {DelaySeconds} 秒后重试。消息保留在RabbitMQ队列中，不会丢失", 
                    retryCount, delay.TotalSeconds);
                
                // 清理可能的部分初始化状态
                await CleanupAsync();
                
                // 持续重试，不设置最大重试次数限制
                // 消息保留在RabbitMQ队列中，服务恢复后会自动处理
                try
                {
                    await Task.Delay(delay, stoppingToken);
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
        
        // 记录存储提供者配置
        _logger.LogInformation("存储提供者: {Provider}", _options.StorageProvider);
        if (_options.StorageProvider.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            if (_options.Elasticsearch != null)
            {
                _logger.LogInformation("Elasticsearch配置 - 索引: {Index}, URLs: {Urls}",
                    _options.Elasticsearch.IndexName, string.Join(", ", _options.Elasticsearch.Urls));
            }
        }
        else if (_options.StorageProvider.Equals("greptimedb", StringComparison.OrdinalIgnoreCase))
        {
            if (_options.GreptimeDB != null)
            {
                _logger.LogInformation("GreptimeDB配置 - 数据库: {Database}, 表: {Table}, URL: {Url}",
                    _options.GreptimeDB.Database, _options.GreptimeDB.TableName, _options.GreptimeDB.Url);
            }
        }
        
        // 初始化存储
        _logger.LogInformation("正在初始化存储服务...");
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditStorageService = scope.ServiceProvider.GetRequiredService<IAuditStorageService>();
            
            var storageInitialized = await auditStorageService.InitializeAsync();
            _logger.LogInformation("存储服务初始化完成，结果: {Result}", storageInitialized);
            
            if (!storageInitialized)
            {
                _logger.LogWarning("存储服务初始化失败，但继续初始化消费者");
            }
        }
        catch (Exception storageEx)
        {
            _logger.LogError(storageEx, "初始化存储服务时发生异常: {Type}: {Message}", 
                storageEx.GetType().Name, storageEx.Message);
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
    /// 处理审计日志消息（批量处理）
    /// </summary>
    /// <remarks>
    /// 消息立即确认，然后异步批量处理
    /// 如果批量处理失败，记录错误日志但不重新入队（因为消息已确认）
    /// </remarks>
    private async Task ProcessAuditLogAsync(Models.AuditLog auditLog)
    {
        var messageId = auditLog?.Id ?? "unknown";
        
        try
        {
            if (auditLog == null)
            {
                _logger.LogError("收到空的审计日志消息");
                throw new ArgumentNullException(nameof(auditLog), "审计日志消息为空");
            }
            
            _logger.LogDebug("收到审计日志消息: ID={Id}, UserId={UserId}, OperationType={OperationType}", 
                messageId, auditLog.UserId, auditLog.OperationType);
            
            // 处理地理位置（异步，不阻塞批量处理）
            _ = Task.Run(async () =>
            {
                try
                {
                    await EnrichWithGeoLocationAsync(auditLog);
                }
                catch (Exception geoEx)
                {
                    _logger.LogWarning(geoEx, "处理地理位置信息失败 - ID: {Id}", messageId);
                }
            });
            
            // 添加到批量缓冲（线程安全）
            bool shouldFlush = false;
            lock (_batchLock)
            {
                _batchBuffer.Add(auditLog);
                
                // 达到批量大小立即刷新
                if (_batchBuffer.Count >= _batchSize)
                {
                    shouldFlush = true;
                    _logger.LogDebug("批量缓冲达到大小限制 ({BatchSize})，立即刷新", _batchSize);
                }
            }
            
            // 异步刷新（不阻塞消息确认）
            if (shouldFlush)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await FlushBatchAsync();
                    }
                    catch (Exception flushEx)
                    {
                        _logger.LogError(flushEx, "批量刷新失败");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理审计日志消息失败 - ID: {Id}", messageId);
            throw; // 重新抛出异常以便RabbitMQ可以重新入队消息
        }
    }
    
    /// <summary>
    /// 刷新批量缓冲
    /// </summary>
    private async Task FlushBatchAsync()
    {
        List<Models.AuditLog> batch;
        
        lock (_batchLock)
        {
            if (_batchBuffer.Count == 0)
                return;
            
            batch = _batchBuffer.ToList();
            _batchBuffer.Clear();
        }
        
        if (batch.Count == 0)
            return;
        
        _logger.LogInformation("开始批量存储审计日志，数量: {Count}", batch.Count);
        
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var auditStorageService = scope.ServiceProvider.GetRequiredService<IAuditStorageService>();
            
            // 批量存储
            var stored = await auditStorageService.BulkStoreAsync(batch);
            
            if (stored)
            {
                _metrics.RecordBatchStored(batch.Count);
                _metrics.IncrementStorageSuccess();
                _logger.LogInformation("批量存储审计日志成功，数量: {Count}", batch.Count);
            }
            else
            {
                _metrics.IncrementStorageFailure();
                _logger.LogError("批量存储审计日志失败，数量: {Count}", batch.Count);
                // 失败时重新添加到缓冲（避免丢失）
                lock (_batchLock)
                {
                    _batchBuffer.InsertRange(0, batch);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量存储审计日志时发生异常，数量: {Count}", batch.Count);
            
            // 失败时重新添加到缓冲（避免丢失）
            lock (_batchLock)
            {
                _batchBuffer.InsertRange(0, batch);
            }
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
    
} 