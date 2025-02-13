using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Repositories
{
    public class AuditLogRepository : Repository<AuditLog, long>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(List<AuditLog> logs, int total)> GetAuditLogsAsync(
            int pageSize, 
            int pageIndex, 
            string userName = null, 
            string eventType = null,
            DateTime[] eventTime = null,
            string ipAddress = null,
            string url = null,
            string method = null,
            int? statusCode = null)
        {
            IQueryable<AuditLog> query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(x => x.UserName.Contains(userName));
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(x => x.EventType.Contains(eventType));
            }

            if (eventTime != null && eventTime.Length == 2)
            {
                query = query.Where(x => x.EventTime >= eventTime[0] && x.EventTime <= eventTime[1]);
            }

            if (!string.IsNullOrEmpty(ipAddress))
            {
                query = query.Where(x => x.IpAddress.Contains(ipAddress));
            }

            if (!string.IsNullOrEmpty(url))
            {
                query = query.Where(x => x.Url.Contains(url));
            }

            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(x => x.Method == method);
            }

            if (statusCode.HasValue)
            {
                query = query.Where(x => x.StatusCode == statusCode.Value);
            }

            int total = await query.CountAsync();

            List<AuditLog> logs = await query
                .OrderByDescending(x => x.EventTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, total);
        }
    }
}