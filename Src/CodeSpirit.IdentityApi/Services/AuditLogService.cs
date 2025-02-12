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
            var (logs, total) = await _auditLogRepository.GetAuditLogsAsync(
                queryDto.PageSize,
                queryDto.Page,
                queryDto.UserName,
                queryDto.EventType);

            return (_mapper.Map<List<AuditLogDto>>(logs), total);
        }

        public async Task<AuditLogDto> GetAuditLogByIdAsync(string id)
        {
            var log = await _auditLogRepository.GetByIdAsync(id);
            if (log == null)
            {
                throw new AppServiceException(404, "审计日志不存在");
            }

            return _mapper.Map<AuditLogDto>(log);
        }
    }
} 