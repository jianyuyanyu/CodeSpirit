using CodeSpirit.Audit.Models.LLM;
using CodeSpirit.Audit.Services.LLM.Dtos;
using CodeSpirit.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CodeSpirit.Audit.Services.LLM.Implementation;

/// <summary>
/// LLM审计服务实现
/// </summary>
public class LLMAuditService : ILLMAuditService
{
    private readonly ILLMAuditStorageService _storageService;
    private readonly ITenantContext? _tenantContext;
    private readonly ILogger<LLMAuditService> _logger;
    private readonly LLMAuditOptions _options;
    private readonly IConnection? _rabbitMqConnection;
    private IModel? _rabbitMqChannel;
    
    /// <summary>
    /// 初始化LLM审计服务
    /// </summary>
    public LLMAuditService(
        ILLMAuditStorageService storageService,
        ITenantContext? tenantContext,
        ILogger<LLMAuditService> logger,
        IOptions<Models.AuditOptions> auditOptions,
        IConnection? rabbitMqConnection = null)
    {
        _storageService = storageService;
        _tenantContext = tenantContext;
        _logger = logger;
        _options = auditOptions.Value.LLMAudit;
        _rabbitMqConnection = rabbitMqConnection;
        
        InitializeRabbitMQ();
    }
    
    /// <summary>
    /// 初始化RabbitMQ
    /// </summary>
    private void InitializeRabbitMQ()
    {
        try
        {
            if (_rabbitMqConnection == null)
            {
                _logger.LogWarning("RabbitMQ连接未配置，将使用同步方式存储LLM审计日志");
                return;
            }
            
            _rabbitMqChannel = _rabbitMqConnection.CreateModel();
            
            // 声明交换机
            _rabbitMqChannel.ExchangeDeclare(
                exchange: _options.RabbitMQ.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);
            
            // 声明队列
            _rabbitMqChannel.QueueDeclare(
                queue: _options.RabbitMQ.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            // 绑定队列到交换机
            _rabbitMqChannel.QueueBind(
                queue: _options.RabbitMQ.QueueName,
                exchange: _options.RabbitMQ.ExchangeName,
                routingKey: _options.RabbitMQ.RoutingKey);
            
            _logger.LogInformation("LLM审计RabbitMQ初始化成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化LLM审计RabbitMQ失败，将使用同步方式存储");
            _rabbitMqChannel = null;
        }
    }
    
    /// <inheritdoc/>
    public async Task LogLLMInteractionAsync(LLMAuditLog auditLog)
    {
        if (!_options.Enabled)
        {
            return;
        }
        
        try
        {
            // 应用敏感数据脱敏
            ApplySensitiveDataMasking(auditLog);
            
            // 截断过长的内容
            TruncateContent(auditLog);
            
            // 计算成本
            CalculateCost(auditLog);
            
            // 使用RabbitMQ异步处理
            if (_rabbitMqChannel != null)
            {
                var message = JsonSerializer.Serialize(auditLog);
                var body = Encoding.UTF8.GetBytes(message);
                
                _rabbitMqChannel.BasicPublish(
                    exchange: _options.RabbitMQ.ExchangeName,
                    routingKey: _options.RabbitMQ.RoutingKey,
                    basicProperties: null,
                    body: body);
                
                _logger.LogDebug("LLM审计日志已发送到消息队列: {Id}", auditLog.Id);
            }
            else
            {
                // 同步存储
                await _storageService.StoreAsync(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录LLM审计日志失败");
        }
    }
    
    /// <inheritdoc/>
    public async Task LogBatchLLMInteractionsAsync(IEnumerable<LLMAuditLog> auditLogs)
    {
        if (!_options.Enabled)
        {
            return;
        }
        
        try
        {
            var logsList = auditLogs.ToList();
            
            foreach (var auditLog in logsList)
            {
                ApplySensitiveDataMasking(auditLog);
                TruncateContent(auditLog);
                CalculateCost(auditLog);
            }
            
            // 批量存储
            await _storageService.BulkStoreAsync(logsList);
            
            _logger.LogInformation("批量记录LLM审计日志成功: {Count}条", logsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量记录LLM审计日志失败");
        }
    }
    
    /// <inheritdoc/>
    public async Task<(IEnumerable<LLMAuditLog> Items, long Total)> SearchAsync(LLMAuditQueryDto query)
    {
        return await _storageService.SearchAsync(query);
    }
    
    /// <inheritdoc/>
    public async Task<LLMAuditLog?> GetByIdAsync(string id)
    {
        return await _storageService.GetByIdAsync(id);
    }
    
    /// <inheritdoc/>
    public async Task<LLMUsageStatsDto> GetUsageStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var stats = new LLMUsageStatsDto();
            
            // 获取总交互次数
            var allInteractions = await _storageService.GetAggregationAsync("isSuccess", startTime, endTime, tenantId);
            stats.TotalInteractions = allInteractions.Values.Sum();
            stats.SuccessfulInteractions = allInteractions.GetValueOrDefault("true", 0);
            stats.FailedInteractions = allInteractions.GetValueOrDefault("false", 0);
            stats.SuccessRate = stats.TotalInteractions > 0 
                ? (double)stats.SuccessfulInteractions / stats.TotalInteractions * 100 
                : 0;
            
            // 按交互类型统计
            stats.InteractionsByType = await _storageService.GetAggregationAsync("interactionType", startTime, endTime, tenantId);
            
            // 按模型统计
            stats.InteractionsByModel = await _storageService.GetAggregationAsync("modelName", startTime, endTime, tenantId);
            
            // 按业务场景统计
            stats.InteractionsByScenario = await _storageService.GetAggregationAsync("businessScenario", startTime, endTime, tenantId);
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM使用统计失败");
            return new LLMUsageStatsDto();
        }
    }
    
    /// <inheritdoc/>
    public async Task<LLMCostStatsDto> GetCostStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var stats = new LLMCostStatsDto();
            
            // TODO: 实现成本统计的具体逻辑
            // 这需要从存储服务中查询并计算成本数据
            
            return await Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM成本统计失败");
            return new LLMCostStatsDto();
        }
    }
    
    /// <inheritdoc/>
    public async Task<LLMQualityStatsDto> GetQualityStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        try
        {
            var stats = new LLMQualityStatsDto();
            
            // TODO: 实现质量统计的具体逻辑
            // 这需要从存储服务中查询并计算质量数据
            
            return await Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM质量统计失败");
            return new LLMQualityStatsDto();
        }
    }
    
    /// <inheritdoc/>
    public async Task<Dictionary<DateTime, long>> GetUsageTrendAsync(DateTime startTime, DateTime endTime, int intervalHours = 24, string? tenantId = null)
    {
        try
        {
            // TODO: 实现使用趋势的具体逻辑
            return await Task.FromResult(new Dictionary<DateTime, long>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取LLM使用趋势失败");
            return new Dictionary<DateTime, long>();
        }
    }
    
    /// <inheritdoc/>
    public async Task<bool> HealthCheckAsync()
    {
        return await _storageService.HealthCheckAsync();
    }
    
    /// <summary>
    /// 应用敏感数据脱敏
    /// </summary>
    private void ApplySensitiveDataMasking(LLMAuditLog auditLog)
    {
        if (!_options.SensitiveData.Enabled)
        {
            return;
        }
        
        // 确保字符串不为null
        auditLog.SystemPrompt ??= string.Empty;
        auditLog.UserPrompt ??= string.Empty;
        auditLog.LLMResponse ??= string.Empty;
        auditLog.ErrorMessage ??= string.Empty;
        
        // 对提示词和响应中的敏感信息进行脱敏
        foreach (var pattern in _options.SensitiveData.SensitiveFieldPatterns)
        {
            auditLog.SystemPrompt = MaskSensitiveData(auditLog.SystemPrompt, pattern);
            auditLog.UserPrompt = MaskSensitiveData(auditLog.UserPrompt, pattern);
            auditLog.LLMResponse = MaskSensitiveData(auditLog.LLMResponse, pattern);
            auditLog.ErrorMessage = MaskSensitiveData(auditLog.ErrorMessage, pattern);
        }
    }
    
    /// <summary>
    /// 脱敏敏感数据
    /// </summary>
    private string MaskSensitiveData(string content, string pattern)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }
        
        // 简单的关键词匹配和替换
        // TODO: 实现更复杂的正则表达式匹配
        return content;
    }
    
    /// <summary>
    /// 截断过长的内容
    /// </summary>
    private void TruncateContent(LLMAuditLog auditLog)
    {
        // 确保字符串不为null
        auditLog.SystemPrompt ??= string.Empty;
        auditLog.UserPrompt ??= string.Empty;
        auditLog.LLMResponse ??= string.Empty;
        auditLog.ProcessedData ??= string.Empty;
        
        if (!_options.LogPrompts)
        {
            auditLog.SystemPrompt = string.Empty;
            auditLog.UserPrompt = string.Empty;
        }
        else
        {
            if (auditLog.SystemPrompt.Length > _options.MaxPromptLength)
            {
                auditLog.SystemPrompt = auditLog.SystemPrompt.Substring(0, _options.MaxPromptLength) + "...";
            }
            if (auditLog.UserPrompt.Length > _options.MaxPromptLength)
            {
                auditLog.UserPrompt = auditLog.UserPrompt.Substring(0, _options.MaxPromptLength) + "...";
            }
        }
        
        if (!_options.LogResponses)
        {
            auditLog.LLMResponse = string.Empty;
        }
        else if (auditLog.LLMResponse.Length > _options.MaxResponseLength)
        {
            auditLog.LLMResponse = auditLog.LLMResponse.Substring(0, _options.MaxResponseLength) + "...";
        }
        
        if (!_options.LogProcessedData)
        {
            auditLog.ProcessedData = string.Empty;
        }
    }
    
    /// <summary>
    /// 计算成本
    /// </summary>
    private void CalculateCost(LLMAuditLog auditLog)
    {
        if (!_options.CostCalculation.Enabled || auditLog.TokenUsage == null)
        {
            _logger.LogWarning(">>> 跳过成本计算：Enabled={Enabled}, TokenUsage={HasValue}", 
                _options.CostCalculation.Enabled, auditLog.TokenUsage != null);
            return;
        }
        
        if (_options.CostCalculation.ModelPricing.TryGetValue(auditLog.ModelName, out var pricing))
        {
            var inputCost = (auditLog.TokenUsage.InputTokens / 1000m) * pricing.InputPer1K;
            var outputCost = (auditLog.TokenUsage.OutputTokens / 1000m) * pricing.OutputPer1K;
            auditLog.CostUsd = inputCost + outputCost;
            
            _logger.LogWarning(">>> 成本计算成功: 输入成本={InputCost:F6}, 输出成本={OutputCost:F6}, 总成本={TotalCost:F6}", 
                inputCost, outputCost, auditLog.CostUsd);
        }
        else
        {
            _logger.LogWarning(">>> 未找到模型 '{ModelName}' 的价格配置！", auditLog.ModelName);
        }
    }
}

