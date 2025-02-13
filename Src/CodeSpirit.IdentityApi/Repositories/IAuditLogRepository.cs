using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IAuditLogRepository : IRepository<AuditLog, long>
    {
        Task<(List<AuditLog> logs, int total)> GetAuditLogsAsync(int pageSize, int pageIndex, string userName = null, string eventType = null, DateTime[] eventTime = null, string ipAddress = null, string url = null, string method = null, int? statusCode = null);
    }
}