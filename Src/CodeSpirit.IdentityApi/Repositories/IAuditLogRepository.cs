using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.Shared.Data;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        Task<(List<AuditLog> logs, int total)> GetAuditLogsAsync(int pageSize, int pageIndex, string userName = null, string eventType = null);
    }
} 