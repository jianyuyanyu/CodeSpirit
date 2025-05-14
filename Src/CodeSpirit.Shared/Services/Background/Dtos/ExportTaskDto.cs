namespace CodeSpirit.Shared.Services.Background.Dtos;

/// <summary>
/// 导出任务DTO
/// </summary>
public class ExportTaskDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; }

    /// <summary>
    /// 文件名称
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 进度百分比
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 已处理记录数
    /// </summary>
    public int ProcessedRecords { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// 文件URL
    /// </summary>
    public string FileUrl { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletionTime { get; set; }
    
    /// <summary>
    /// 错误消息列表
    /// </summary>
    public List<string> ErrorMessages { get; set; }
} 