using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Shared.Services.Background;

/// <summary>
/// 后台任务
/// </summary>
public class BackgroundJob
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// 任务处理函数
    /// </summary>
    public Func<IServiceScopeFactory, CancellationToken, Task> Job { get; set; }
    
    /// <summary>
    /// 任务状态
    /// </summary>
    public JobStatus Status { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; }
} 