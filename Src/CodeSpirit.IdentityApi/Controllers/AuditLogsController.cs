using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Controllers.Dtos.AuditLog;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("审计日志")]
    [Page(Label = "审计日志", ParentLabel = "系统管理", Icon = "fa-solid fa-list-check", PermissionCode = PermissionCodes.AuditLogManagement)]
    [Permission(code: PermissionCodes.AuditLogManagement)]
    public class AuditLogsController : ApiControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogQueryDto queryDto)
        {
            var (logs, total) = await _auditLogService.GetAuditLogsAsync(queryDto);
            return SuccessResponse(new ListData<AuditLogDto>
            {
                Items = logs,
                Total = total
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLog(string id)
        {
            var log = await _auditLogService.GetAuditLogByIdAsync(id);
            return SuccessResponse(log);
        }
    }
} 