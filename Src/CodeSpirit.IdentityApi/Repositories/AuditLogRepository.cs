using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeSpirit.IdentityApi.Repositories
{
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<(List<AuditLog> logs, int total)> GetAuditLogsAsync(int pageSize, int pageIndex, string userName = null, string eventType = null)
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