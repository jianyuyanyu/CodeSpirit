using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Shared.Performance;

/// <summary>
/// 线程池配置管理
/// </summary>
/// <remarks>
/// 用于在分布式环境中根据服务等级和实例数量动态配置线程池
/// </remarks>
public static class ThreadPoolConfiguration
{
    /// <summary>
    /// 服务等级枚举
    /// </summary>
    public enum ServiceTier
    {
        /// <summary>
        /// 高并发服务（如ExamApi、IdentityApi、Web前端）
        /// </summary>
        High,
        
        /// <summary>
        /// 中等并发服务（如FileStorage、Survey、Messaging）
        /// </summary>
        Medium,
        
        /// <summary>
        /// 低并发服务（如ConfigCenter、Approval）
        /// </summary>
        Low
    }

    /// <summary>
    /// 根据服务等级配置线程池
    /// </summary>
    /// <param name="serviceTier">服务等级</param>
    /// <param name="expectedInstances">预期实例数量（默认3副本）</param>
    /// <param name="logger">日志记录器（可选）</param>
    public static void ConfigureThreadPool(ServiceTier serviceTier, int expectedInstances = 3, ILogger? logger = null)
    {
        // 基础线程数配置（假设总并发500）
        var baseThreads = serviceTier switch
        {
            ServiceTier.High => 80,    // 高并发服务
            ServiceTier.Medium => 50,  // 中等并发服务
            ServiceTier.Low => 30,     // 低并发服务
            _ => 50
        };

        // 根据实例数量调整（避免过度配置）
        var adjustedThreads = Math.Max(30, baseThreads);

        ThreadPool.GetMinThreads(out int currentWorkerThreads, out int currentCompletionPortThreads);
        
        var newWorkerThreads = Math.Max(currentWorkerThreads, adjustedThreads);
        var newCompletionPortThreads = Math.Max(currentCompletionPortThreads, adjustedThreads);
        
        ThreadPool.SetMinThreads(newWorkerThreads, newCompletionPortThreads);

        logger?.LogInformation(
            "线程池配置完成 - 服务等级: {ServiceTier}, 预期实例数: {ExpectedInstances}, " +
            "工作线程: {CurrentWorkerThreads} -> {NewWorkerThreads}, " +
            "IO完成端口线程: {CurrentCompletionPortThreads} -> {NewCompletionPortThreads}",
            serviceTier, expectedInstances, 
            currentWorkerThreads, newWorkerThreads,
            currentCompletionPortThreads, newCompletionPortThreads);
    }

    /// <summary>
    /// 从配置中读取并配置线程池
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="logger">日志记录器（可选）</param>
    public static void ConfigureFromConfiguration(IConfiguration configuration, ILogger? logger = null)
    {
        var serviceTierStr = configuration["Performance:ServiceTier"];
        var expectedInstances = configuration.GetValue<int?>("Performance:ExpectedInstances") ?? 3;
        var minWorkerThreads = configuration.GetValue<int?>("Performance:MinWorkerThreads");

        // 如果明确指定了最小线程数，直接使用
        if (minWorkerThreads.HasValue)
        {
            ThreadPool.GetMinThreads(out int wt, out int cpt);
            ThreadPool.SetMinThreads(
                Math.Max(wt, minWorkerThreads.Value),
                Math.Max(cpt, minWorkerThreads.Value)
            );

            logger?.LogInformation(
                "线程池配置完成（手动配置） - 最小线程数: {MinThreads}",
                minWorkerThreads.Value);
            return;
        }

        // 否则根据服务等级自动配置
        if (Enum.TryParse<ServiceTier>(serviceTierStr, true, out var serviceTier))
        {
            ConfigureThreadPool(serviceTier, expectedInstances, logger);
        }
        else
        {
            // 默认使用中等级别
            ConfigureThreadPool(ServiceTier.Medium, expectedInstances, logger);
        }
    }

    /// <summary>
    /// 获取当前线程池配置信息
    /// </summary>
    /// <returns>包含当前线程池配置的字典</returns>
    public static Dictionary<string, int> GetCurrentConfiguration()
    {
        ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
        ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);

        return new Dictionary<string, int>
        {
            ["MinWorkerThreads"] = minWorkerThreads,
            ["MinCompletionPortThreads"] = minCompletionPortThreads,
            ["MaxWorkerThreads"] = maxWorkerThreads,
            ["MaxCompletionPortThreads"] = maxCompletionPortThreads,
            ["AvailableWorkerThreads"] = availableWorkerThreads,
            ["AvailableCompletionPortThreads"] = availableCompletionPortThreads
        };
    }
}

