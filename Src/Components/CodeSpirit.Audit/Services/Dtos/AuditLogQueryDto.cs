namespace CodeSpirit.Audit.Services.Dtos;

/// <summary>
/// 审计日志查询参数
/// </summary>
public class AuditLogQueryDto
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// 控制器名称
    /// </summary>
    public string ControllerName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作名称
    /// </summary>
    public string ActionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体名称
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    
    /// <summary>
    /// 实体ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool? IsSuccess { get; set; }
    
    /// <summary>
    /// 关键字搜索
    /// </summary>
    public string Keyword { get; set; } = string.Empty;
    
    /// <summary>
    /// 排序字段
    /// </summary>
    public string SortField { get; set; } = "OperationTime";
    
    /// <summary>
    /// 排序方向
    /// </summary>
    public string SortDirection { get; set; } = "desc";
} 