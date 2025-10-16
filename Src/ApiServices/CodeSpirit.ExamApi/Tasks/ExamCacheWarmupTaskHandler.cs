using CodeSpirit.ExamApi.Services.Interfaces;
using CodeSpirit.ScheduledTasks.Services;
using CodeSpirit.ScheduledTasks.Models;

namespace CodeSpirit.ExamApi.Tasks;

/// <summary>
/// 考试缓存预热任务处理器
/// </summary>
public class ExamCacheWarmupTaskHandler : ITaskHandler
{
    private readonly IExamCacheService _examCacheService;
    private readonly ILogger<ExamCacheWarmupTaskHandler> _logger;

    /// <summary>
    /// 初始化考试缓存预热任务处理器
    /// </summary>
    /// <param name="examCacheService">考试缓存服务</param>
    /// <param name="logger">日志记录器</param>
    public ExamCacheWarmupTaskHandler(
        IExamCacheService examCacheService,
        ILogger<ExamCacheWarmupTaskHandler> logger)
    {
        _examCacheService = examCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 执行考试缓存预热任务
    /// </summary>
    /// <param name="parameters">任务参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    public async Task<string?> ExecuteAsync(string? parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始执行考试缓存预热任务");

        try
        {
            // 检查取消令牌
            cancellationToken.ThrowIfCancellationRequested();

            // 执行考试缓存预热
            await _examCacheService.WarmupUpcomingExamsCacheAsync();

            _logger.LogInformation("考试缓存预热任务执行成功");
            
            return "考试缓存预热任务执行成功";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("考试缓存预热任务被取消");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "考试缓存预热任务执行失败");
            throw;
        }
    }
}
