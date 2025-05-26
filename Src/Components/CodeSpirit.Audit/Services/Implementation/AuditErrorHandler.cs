using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CodeSpirit.Audit.Services.Implementation;

/// <summary>
/// 审计错误处理服务
/// </summary>
public interface IAuditErrorHandler
{
    /// <summary>
    /// 处理审计错误
    /// </summary>
    Task HandleErrorAsync(Exception exception, string operation, object? context = null);
    
    /// <summary>
    /// 执行带重试的操作
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3);
    
    /// <summary>
    /// 执行带重试的操作（无返回值）
    /// </summary>
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3);
}

/// <summary>
/// 审计错误处理服务实现
/// </summary>
public class AuditErrorHandler : IAuditErrorHandler
{
    private readonly ILogger<AuditErrorHandler> _logger;
    
    public AuditErrorHandler(ILogger<AuditErrorHandler> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// 处理审计错误
    /// </summary>
    public async Task HandleErrorAsync(Exception exception, string operation, object? context = null)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogError(exception, 
            "审计操作失败 [错误ID: {ErrorId}] - 操作: {Operation}, 上下文: {Context}",
            errorId, operation, context?.ToString() ?? "无");
        
        // 根据异常类型进行不同的处理
        switch (exception)
        {
            case TimeoutException:
                _logger.LogWarning("审计操作超时 [错误ID: {ErrorId}] - 操作: {Operation}", errorId, operation);
                break;
                
            case UnauthorizedAccessException:
                _logger.LogWarning("审计操作权限不足 [错误ID: {ErrorId}] - 操作: {Operation}", errorId, operation);
                break;
                
            case InvalidOperationException:
                _logger.LogWarning("审计操作状态无效 [错误ID: {ErrorId}] - 操作: {Operation}", errorId, operation);
                break;
                
            default:
                _logger.LogError("审计操作发生未知错误 [错误ID: {ErrorId}] - 操作: {Operation}", errorId, operation);
                break;
        }
        
        // 这里可以添加错误通知、指标收集等逻辑
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 执行带重试的操作
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, int maxRetries = 3)
    {
        var attempt = 0;
        Exception? lastException = null;
        
        while (attempt < maxRetries)
        {
            try
            {
                attempt++;
                _logger.LogDebug("执行审计操作 '{OperationName}' - 尝试 {Attempt}/{MaxRetries}", 
                    operationName, attempt, maxRetries);
                
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("审计操作 '{OperationName}' 在第 {Attempt} 次尝试后成功", 
                        operationName, attempt);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex, "审计操作 '{OperationName}' 在 {MaxRetries} 次尝试后仍然失败", 
                        operationName, maxRetries);
                    break;
                }
                
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000); // 指数退避
                _logger.LogWarning(ex, "审计操作 '{OperationName}' 第 {Attempt} 次尝试失败，{Delay}ms 后重试", 
                    operationName, attempt, delay.TotalMilliseconds);
                
                await Task.Delay(delay);
            }
        }
        
        // 处理最终失败
        if (lastException != null)
        {
            await HandleErrorAsync(lastException, operationName);
            throw new AuditOperationException($"审计操作 '{operationName}' 在 {maxRetries} 次重试后失败", lastException);
        }
        
        throw new InvalidOperationException("不应该到达这里");
    }
    
    /// <summary>
    /// 执行带重试的操作（无返回值）
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, int maxRetries = 3)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, maxRetries);
    }
}

/// <summary>
/// 审计操作异常
/// </summary>
public class AuditOperationException : Exception
{
    public AuditOperationException(string message) : base(message) { }
    public AuditOperationException(string message, Exception innerException) : base(message, innerException) { }
} 