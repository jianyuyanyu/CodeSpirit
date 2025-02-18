using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Dtos.AuditLog;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IAuditLogService : IScopedDependency
    {
        Task<AuditLogDto> GetAuditLogByIdAsync(long id);
        Task<PageList<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto queryDto);
    }
}