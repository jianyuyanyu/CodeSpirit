namespace CodeSpirit.PdfGeneration.Options;

/// <summary>
/// PDF生成服务的配置选项
/// </summary>
public class PdfGenerationOptions
{
    /// <summary>
    /// 最大并发任务数
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 5;
    
    /// <summary>
    /// 浏览器池大小
    /// </summary>
    public int BrowserPoolSize { get; set; } = 3;
    
    /// <summary>
    /// 浏览器超时时间
    /// </summary>
    public TimeSpan BrowserTimeout { get; set; } = TimeSpan.FromMinutes(2);
    
    /// <summary>
    /// 浏览器启动参数
    /// </summary>
    public string[] BrowserArguments { get; set; } = new[]
    {
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-dev-shm-usage",
        "--disable-gpu",
        "--no-first-run",
        "--no-zygote",
        "--single-process"
    };
    
    /// <summary>
    /// 浏览器可执行文件路径，如果为null则使用默认路径
    /// </summary>
    public string? ExecutablePath { get; set; }
    
    /// <summary>
    /// 是否禁用浏览器头部
    /// </summary>
    public bool Headless { get; set; } = true;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// 重试延迟
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// 浏览器进程内存限制（MB）
    /// </summary>
    public int? BrowserMemoryLimit { get; set; } = 512;
}