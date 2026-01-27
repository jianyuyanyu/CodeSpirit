using CodeSpirit.Audit.Services.Dtos;

namespace CodeSpirit.Audit.Services;

/// <summary>
/// 审计查询服务接口
/// </summary>
/// <remarks>
/// 专门负责审计日志的查询功能，职责单一
/// </remarks>
public interface IAuditQueryService
{
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
    /// 删除审计日志
    /// </summary>
    /// <param name="id">审计日志ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(string id);
}
