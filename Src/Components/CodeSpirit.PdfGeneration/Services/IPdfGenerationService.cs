using PuppeteerSharp;

namespace CodeSpirit.PdfGeneration.Services;

/// <summary>
/// PDF生成服务接口
/// </summary>
public interface IPdfGenerationService : IAsyncDisposable
{
    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 从HTML内容生成PDF
    /// </summary>
    /// <param name="htmlContent">HTML内容</param>
    /// <param name="options">PDF生成选项</param>
    /// <returns>PDF文件字节数组</returns>
    Task<byte[]> GeneratePdfAsync(string htmlContent, PdfOptions? options = null);
    
    /// <summary>
    /// 从HTML内容批量生成PDF
    /// </summary>
    /// <param name="htmlContents">HTML内容列表</param>
    /// <param name="options">PDF生成选项</param>
    /// <returns>PDF文件字节数组列表</returns>
    Task<IList<byte[]>> GeneratePdfBatchAsync(IEnumerable<string> htmlContents, PdfOptions? options = null);
    
    /// <summary>
    /// 获取服务状态信息
    /// </summary>
    /// <returns>服务状态信息</returns>
    Task<PdfGenerationServiceStatus> GetStatusAsync();
}

/// <summary>
/// PDF生成服务状态
/// </summary>
public class PdfGenerationServiceStatus
{
    /// <summary>
    /// 是否已初始化
    /// </summary>
    public bool IsInitialized { get; set; }
    
    /// <summary>
    /// 当前活动的任务数
    /// </summary>
    public int ActiveTasks { get; set; }
    
    /// <summary>
    /// 浏览器池大小
    /// </summary>
    public int PoolSize { get; set; }
    
    /// <summary>
    /// 可用浏览器实例数
    /// </summary>
    public int AvailableBrowsers { get; set; }
    
    /// <summary>
    /// 服务运行时间
    /// </summary>
    public TimeSpan Uptime { get; set; }
    
    /// <summary>
    /// 总处理任务数
    /// </summary>
    public long TotalTasksProcessed { get; set; }
    
    /// <summary>
    /// 失败任务数
    /// </summary>
    public long FailedTasks { get; set; }
}