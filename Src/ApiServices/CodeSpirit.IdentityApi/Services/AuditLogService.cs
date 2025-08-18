using AutoMapper;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.AuditLog;
using CodeSpirit.Shared.Repositories;
using LinqKit;

namespace CodeSpirit.IdentityApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IMapper _mapper;

        public AuditLogService(IRepository<AuditLog> auditLogRepository, IMapper mapper)
        {
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
        }

        public async Task<PageList<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryDto queryDto)
        {
            ExpressionStarter<AuditLog> predicate = PredicateBuilder.New<AuditLog>(true);
            if (!string.IsNullOrEmpty(queryDto.UserName))
            {
                predicate = predicate.And(x => x.UserName.Contains(queryDto.UserName));
            }

            if (!string.IsNullOrEmpty(queryDto.EventType))
            {
                predicate = predicate.And(x => x.EventType.Contains(queryDto.EventType));
            }

            if (queryDto.EventTime != null && queryDto.EventTime.Length == 2)
            {
                predicate = predicate.And(x => x.EventTime >= queryDto.EventTime[0] && x.EventTime <= queryDto.EventTime[1]);
            }
            if (!string.IsNullOrEmpty(queryDto.IpAddress))
            {
                predicate = predicate.And(x => x.IpAddress.Contains(queryDto.IpAddress));
            }
            if (!string.IsNullOrEmpty(queryDto.Url))
            {
                predicate = predicate.And(x => x.Url.Contains(queryDto.Url));
            }

            if (!string.IsNullOrEmpty(queryDto.Method))
            {
                predicate = predicate.And(x => x.Method == queryDto.Method);
            }

            if (queryDto.StatusCode.HasValue)
            {
                predicate = predicate.And(x => x.StatusCode == queryDto.StatusCode.Value);
            }

            if (string.IsNullOrEmpty(queryDto.OrderBy))
            {
                queryDto.OrderBy = "EventTime";
                queryDto.OrderDir = "desc";
            }

            PageList<AuditLog> result = await _auditLogRepository.GetPagedAsync(
                queryDto.Page,
                queryDto.PerPage,
                predicate,
                queryDto.OrderBy,
                queryDto.OrderDir
            );
            return _mapper.Map<PageList<AuditLogDto>>(result);
        }

        public async Task<AuditLogDto> GetAuditLogByIdAsync(long id)
        {
            AuditLog log = await _auditLogRepository.GetByIdAsync(id);
            return log == null ? throw new AppServiceException(404, "审计日志不存在") : _mapper.Map<AuditLogDto>(log);
        }
    }
}