using CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IAuditLogService
    {
        Task<(List<AuditLogDto> logs, int total)> GetAuditLogsAsync(AuditLogQueryDto queryDto);
        Task<AuditLogDto> GetAuditLogByIdAsync(long id);
    }
}