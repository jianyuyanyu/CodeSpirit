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
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<AuditLogController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">审计服务</param>
        /// <param name="elasticsearchService">Elasticsearch服务</param>
        /// <param name="logger">日志记录器</param>
        public AuditLogController(
            IAuditService auditService,
            IElasticsearchService elasticsearchService,
            ILogger<AuditLogController> logger)
        {
            _auditService = auditService;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        /// <summary>
        /// 查询审计日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        [DisplayName("查询审计日志")]
        public async Task<ActionResult<ApiResponse<PageList<CodeSpirit.Audit.Models.AuditLog>>>> GetAsync([FromQuery] AuditLogQueryDto query)
        {
            try
            {
                // 设置默认排序 - 使用OperationTime字段名与模型保持一致
                if (string.IsNullOrEmpty(query.SortField))
                {
                    query.SortField = "OperationTime";
                    query.SortDirection = "desc";
                }

                var (items, total) = await _auditService.SearchAsync(query);

                var result = new PageList<CodeSpirit.Audit.Models.AuditLog>(
                    items.ToList(), 
                    (int)total
                );

                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询审计日志失败");
                return BadResponse<PageList<CodeSpirit.Audit.Models.AuditLog>>("查询审计日志失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 根据ID获取审计日志详情
        /// </summary>
        /// <param name="id">审计日志ID</param>
        /// <returns>审计日志详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取审计日志详情")]
        public async Task<ActionResult<ApiResponse<CodeSpirit.Audit.Models.AuditLog>>> GetByIdAsync(string id)
        {
            try
            {
                var auditLog = await _auditService.GetByIdAsync(id);
                if (auditLog == null)
                {
                    return BadResponse<CodeSpirit.Audit.Models.AuditLog>("未找到指定的审计日志");
                }

                return SuccessResponse(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志详情失败");
                return BadResponse<CodeSpirit.Audit.Models.AuditLog>("获取审计日志详情失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 重建Elasticsearch索引
        /// </summary>
        /// <returns>操作结果</returns>
        [HttpPost("rebuild-index")]
        [DisplayName("重建Elasticsearch索引")]
        public async Task<ActionResult<ApiResponse>> RebuildIndexAsync()
        {
            try
            {
                _logger.LogInformation("开始重建Elasticsearch索引");
                
                var success = await _elasticsearchService.RecreateIndexAsync();
                
                if (success)
                {
                    _logger.LogInformation("Elasticsearch索引重建成功");
                    return SuccessResponse("Elasticsearch索引重建成功！现在应该可以正常进行排序和搜索操作。");
                }
                else
                {
                    _logger.LogError("Elasticsearch索引重建失败");
                    return BadResponse("Elasticsearch索引重建失败，请查看日志获取详细信息。");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重建Elasticsearch索引时发生异常");
                return BadResponse($"重建Elasticsearch索引时发生异常: {ex.Message}");
            }
        }
    }
}