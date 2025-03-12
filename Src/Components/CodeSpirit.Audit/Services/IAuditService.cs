namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计服务接口
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="auditLog">审计日志</param>
    /// <returns>任务</returns>
    Task LogAsync(Models.AuditLog auditLog);
    
    /// <summary>
    /// 根据ID获取审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>审计日志</returns>
    Task<Models.AuditLog?> GetByIdAsync(string id);
    
    /// <summary>
    /// 搜索审计日志
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>审计日志列表</returns>
    Task<(IEnumerable<Models.AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query);
    
    /// <summary>
    /// 获取操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>统计信息</returns>
    Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// 获取用户操作统计信息
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="topN">前N个用户</param>
    /// <returns>用户统计信息</returns>
    Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10);
    
    /// <summary>
    /// 根据时间获取操作趋势
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="interval">时间间隔(小时)</param>
    /// <returns>操作趋势</returns>
    Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24);
}

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