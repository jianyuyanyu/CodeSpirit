using Audit.WebApi;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.AuditLog;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("审计日志")]
    [Navigation(Icon = "fa-solid fa-list-check", PlatformType = PlatformType.Both)]
    [AuditIgnore]
    public class AuditLogsController : ApiControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [DisplayName("获取审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogQueryDto queryDto)
        {
            PageList<AuditLogDto> results = await _auditLogService.GetAuditLogsAsync(queryDto);
            return SuccessResponse(results);
        }

        [HttpGet("{id}")]
        [DisplayName("获取审计日志详情")]
        public async Task<ActionResult<ApiResponse<AuditLogDto>>> Detail(long id)
        {
            AuditLogDto log = await _auditLogService.GetAuditLogByIdAsync(id);
            return SuccessResponse(log);
        }
    }
}