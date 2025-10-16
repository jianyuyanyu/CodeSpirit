using CodeSpirit.ScheduledTasks.Services;

namespace CodeSpirit.ScheduledTasks.Examples;

/// <summary>
/// 示例任务处理器
/// </summary>
public class SampleTaskHandler : ITaskHandler
{
    private readonly ILogger<SampleTaskHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public SampleTaskHandler(ILogger<SampleTaskHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行示例任务，参数: {Parameters}", parameters);

        try
        {
            // 模拟任务执行
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            var result = $"示例任务执行完成，执行时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            
            _logger.LogInformation("示例任务执行成功: {Result}", result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("示例任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "示例任务执行失败");
            throw;
        }
    }
}

/// <summary>
/// 数据清理任务处理器
/// </summary>
public class DataCleanupTaskHandler : ITaskHandler
{
    private readonly ILogger<DataCleanupTaskHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public DataCleanupTaskHandler(ILogger<DataCleanupTaskHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行数据清理任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行数据清理任务");

        try
        {
            // 解析参数
            var cleanupDays = 30; // 默认清理30天前的数据
            if (!string.IsNullOrEmpty(parameters))
            {
                var paramDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters);
                if (paramDict?.ContainsKey("cleanupDays") == true)
                {
                    cleanupDays = Convert.ToInt32(paramDict["cleanupDays"]);
                }
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-cleanupDays);
            _logger.LogInformation("清理 {CutoffDate} 之前的数据", cutoffDate);

            // 模拟数据清理过程
            var cleanedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 模拟清理操作
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                cleanedCount += Random.Shared.Next(1, 100);
                
                _logger.LogDebug("已清理 {Count} 条记录", cleanedCount);
            }

            var result = $"数据清理完成，共清理 {cleanedCount} 条记录";
            _logger.LogInformation(result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("数据清理任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据清理任务执行失败");
            throw;
        }
    }
}

/// <summary>
/// 报表生成任务处理器
/// </summary>
public class ReportGenerationTaskHandler : ITaskHandler<ReportParameters>
{
    private readonly ILogger<ReportGenerationTaskHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public ReportGenerationTaskHandler(ILogger<ReportGenerationTaskHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行报表生成任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        ReportParameters? reportParams = null;
        
        if (!string.IsNullOrEmpty(parameters))
        {
            reportParams = JsonConvert.DeserializeObject<ReportParameters>(parameters);
        }

        return await ExecuteAsync(reportParams, cancellationToken);
    }

    /// <summary>
    /// 执行报表生成任务
    /// </summary>
    /// <param name="parameters">强类型参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(ReportParameters? parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始生成报表");

        try
        {
            var reportType = parameters?.ReportType ?? "默认报表";
            var startDate = parameters?.StartDate ?? DateTime.UtcNow.AddDays(-7);
            var endDate = parameters?.EndDate ?? DateTime.UtcNow;

            _logger.LogInformation("生成报表类型: {ReportType}, 时间范围: {StartDate} - {EndDate}", 
                reportType, startDate, endDate);

            // 模拟报表生成过程
            var steps = new[] { "数据收集", "数据处理", "图表生成", "报表编译", "文件保存" };
            
            foreach (var step in steps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("正在执行: {Step}", step);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            var reportPath = $"/reports/{reportType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var result = $"报表生成完成，文件路径: {reportPath}";
            
            _logger.LogInformation(result);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("报表生成任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报表生成任务执行失败");
            throw;
        }
    }
}

/// <summary>
/// 报表参数
/// </summary>
public class ReportParameters
{
    /// <summary>
    /// 报表类型
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 其他参数
    /// </summary>
    public Dictionary<string, object>? AdditionalParameters { get; set; }
}
