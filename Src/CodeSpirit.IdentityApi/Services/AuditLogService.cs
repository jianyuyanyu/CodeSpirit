using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Repositories;

namespace CodeSpirit.IdentityApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IMapper _mapper;

        public AuditLogService(IAuditLogRepository auditLogRepository, IMapper mapper)
        {
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
        }

        public async Task<(List<AuditLogDto> logs, int total)> GetAuditLogsAsync(AuditLogQueryDto queryDto)
        {
            (List<AuditLog> logs, int total) = await _auditLogRepository.GetAuditLogsAsync(
                queryDto.PageSize,
                queryDto.Page,
                queryDto.UserName,
                queryDto.EventType,
                queryDto.EventTime,
                queryDto.IpAddress,
                queryDto.Url,
                queryDto.Method,
                queryDto.StatusCode);

            return (_mapper.Map<List<AuditLogDto>>(logs), total);
        }

        public async Task<AuditLogDto> GetAuditLogByIdAsync(long id)
        {
            AuditLog log = await _auditLogRepository.GetByIdAsync(id);
            return log == null ? throw new AppServiceException(404, "审计日志不存在") : _mapper.Map<AuditLogDto>(log);
        }

    }
}