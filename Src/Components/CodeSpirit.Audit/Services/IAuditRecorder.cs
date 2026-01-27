namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计记录服务接口
/// </summary>
/// <remarks>
/// 专门负责审计日志的记录功能，职责单一
/// </remarks>
public interface IAuditRecorder
{
    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="auditLog">审计日志</param>
    /// <returns>任务</returns>
    Task RecordAsync(Models.AuditLog auditLog);
}
