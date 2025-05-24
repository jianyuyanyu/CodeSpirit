using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 审计日志控制器
    /// </summary>
    [DisplayName("审计日志")]
    [Navigation(Icon = "fa-solid fa-clipboard-list")]
    public class AuditLogController : ApiControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditLogController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">审计服务</param>
        /// <param name="logger">日志记录器</param>
        public AuditLogController(
            IAuditService auditService,
            ILogger<AuditLogController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// 查询审计日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        [DisplayName("查询审计日志")]
        public async Task<ActionResult<ApiResponse>> GetAsync([FromQuery] AuditLogQueryDto query)
        {
            try
            {
                // 设置默认排序
                if (string.IsNullOrEmpty(query.SortField))
                {
                    query.SortField = "operationTime";
                    query.SortDirection = "desc";
                }

                var (items, total) = await _auditService.SearchAsync(query);
                
                var result = new 
                {
                    Items = items,
                    TotalCount = total,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize
                };

                return SuccessResponse(result as object as dynamic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询审计日志失败");
                return BadResponse("查询审计日志失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 根据ID获取审计日志详情
        /// </summary>
        /// <param name="id">审计日志ID</param>
        /// <returns>审计日志详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取审计日志详情")]
        public async Task<ActionResult<ApiResponse>> GetByIdAsync(string id)
        {
            try
            {
                var auditLog = await _auditService.GetByIdAsync(id);
                if (auditLog == null)
                {
                    return BadResponse("未找到指定的审计日志");
                }

                return SuccessResponse(auditLog as object as dynamic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志详情失败");
                return BadResponse("获取审计日志详情失败: " + ex.Message);
            }
        }
    }
} 