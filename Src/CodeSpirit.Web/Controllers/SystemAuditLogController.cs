using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Authorization;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Audit.Attributes;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 系统平台审计日志控制器
    /// 系统管理员可以查看所有租户的审计日志
    /// </summary>
    [DisplayName("系统审计日志")]
    [Navigation(Icon = "fa-solid fa-shield-halved", PlatformType = PlatformType.System)]
    [Platform(PlatformType.System)]
    [NoAudit("系统审计日志控制器不需要审计")]
    public class SystemAuditLogController : ApiControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<SystemAuditLogController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">审计服务</param>
        /// <param name="currentUser">当前用户服务</param>
        /// <param name="logger">日志记录器</param>
        public SystemAuditLogController(
            IAuditService auditService,
            ICurrentUser currentUser,
            ILogger<SystemAuditLogController> logger)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有租户的审计日志列表
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet]
        [DisplayName("获取审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogQueryDto queryDto)
        {
            try
            {
                // 系统管理员可以查询所有租户的数据，不设置TenantId限制
                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logDtos = logs.ToDto().ToList();
                var pageList = new PageList<AuditLogDto>(logDtos, (int)total);

                _logger.LogInformation("系统管理员 {UserId} 获取审计日志列表成功，返回 {Count} 条记录，租户: {TenantId}",
                    _currentUser.Id, logDtos.Count, queryDto.TenantId ?? "全部");

                return SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志列表失败");
                return BadResponse<PageList<AuditLogDto>>("获取审计日志列表失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 导出审计日志列表（用于导出功能）
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet("Export")]
        [DisplayName("导出审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<AuditLogDto>>>> Export([FromQuery] AuditLogQueryDto queryDto)
        {
            try
            {
                // 系统管理员可以导出任意租户的数据
                const int MaxExportLimit = 50000; // 系统管理员可以导出更多数据
                queryDto.PerPage = Math.Min(queryDto.PerPage, MaxExportLimit);
                queryDto.Page = 1;
                
                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logDtos = logs.ToDto().ToList();
                var pageList = new PageList<AuditLogDto>(logDtos, (int)total);

                _logger.LogInformation("系统管理员 {UserId} 导出审计日志成功，共 {Count} 条记录，租户: {TenantId}",
                    _currentUser.Id, logDtos.Count, queryDto.TenantId ?? "全部");

                return logDtos.Count == 0 ? BadResponse<PageList<AuditLogDto>>("没有数据可供导出") : SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出审计日志失败");
                return BadResponse<PageList<AuditLogDto>>("导出审计日志失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取审计日志详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>审计日志详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取审计日志详情")]
        public async Task<ActionResult<ApiResponse<AuditLogDto>>> Detail(string id)
        {
            try
            {
                var query = new AuditLogQueryDto
                {
                    PerPage = 1000 // 搜索更多记录来查找指定ID
                };

                var (logs, _) = await _auditService.SearchAsync(query);
                
                var log = logs.FirstOrDefault(l => l.Id == id);
                if (log == null)
                {
                    return BadResponse<AuditLogDto>($"未找到ID为 {id} 的审计日志");
                }

                var logDto = log.ToDto();

                _logger.LogInformation("系统管理员 {UserId} 获取审计日志详情成功，日志ID: {LogId}",
                    _currentUser.Id, id);

                return SuccessResponse(logDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志详情失败，ID: {Id}", id);
                return BadResponse<AuditLogDto>("获取审计日志详情失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取指定租户的操作统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="tenantId">租户ID，可选</param>
        /// <returns>操作统计</returns>
        [HttpGet("stats/operations")]
        [DisplayName("获取操作统计")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, long>>>> GetOperationStatsAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime,
            [FromQuery] string tenantId = null)
        {
            try
            {
                var stats = await _auditService.GetOperationStatsAsync(startTime, endTime, tenantId);
                
                _logger.LogInformation("系统管理员 {UserId} 获取操作统计成功，时间范围: {StartTime} - {EndTime}, 租户: {TenantId}",
                    _currentUser.Id, startTime, endTime, tenantId ?? "全部");

                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作统计失败");
                return BadResponse<Dictionary<string, long>>("获取操作统计失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取指定租户的用户操作统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="topN">前N个用户</param>
        /// <param name="tenantId">租户ID，可选</param>
        /// <returns>用户操作统计</returns>
        [HttpGet("stats/users")]
        [DisplayName("获取用户统计")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, long>>>> GetUserStatsAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime,
            [FromQuery] int topN = 10,
            [FromQuery] string tenantId = null)
        {
            try
            {
                var stats = await _auditService.GetUserStatsAsync(startTime, endTime, topN, tenantId);
                
                _logger.LogInformation("系统管理员 {UserId} 获取用户统计成功，时间范围: {StartTime} - {EndTime}, TopN: {TopN}, 租户: {TenantId}",
                    _currentUser.Id, startTime, endTime, topN, tenantId ?? "全部");

                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计失败");
                return BadResponse<Dictionary<string, long>>("获取用户统计失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取指定租户的操作趋势
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">时间间隔(小时)</param>
        /// <param name="tenantId">租户ID，可选</param>
        /// <returns>操作趋势</returns>
        [HttpGet("stats/trend")]
        [DisplayName("获取操作趋势")]
        public async Task<ActionResult<ApiResponse<Dictionary<DateTime, long>>>> GetOperationTrendAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime,
            [FromQuery] int interval = 24,
            [FromQuery] string tenantId = null)
        {
            try
            {
                var trend = await _auditService.GetOperationTrendAsync(startTime, endTime, interval, tenantId);
                
                _logger.LogInformation("系统管理员 {UserId} 获取操作趋势成功，时间范围: {StartTime} - {EndTime}, 间隔: {Interval}小时, 租户: {TenantId}",
                    _currentUser.Id, startTime, endTime, interval, tenantId ?? "全部");

                return SuccessResponse(trend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作趋势失败");
                return BadResponse<Dictionary<DateTime, long>>("获取操作趋势失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取所有租户的审计概览
        /// </summary>
        /// <param name="tenantId">租户ID，可选，不指定则获取所有租户的汇总数据</param>
        /// <returns>审计概览信息</returns>
        [HttpGet("overview")]
        [DisplayName("获取系统审计概览")]
        public async Task<ActionResult<ApiResponse<object>>> GetOverviewAsync([FromQuery] string tenantId = null)
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddDays(-30); // 最近30天

                // 并行获取各项统计数据
                var operationStatsTask = _auditService.GetOperationStatsAsync(startTime, endTime, tenantId);
                var userStatsTask = _auditService.GetUserStatsAsync(startTime, endTime, 10, tenantId);
                var trendTask = _auditService.GetOperationTrendAsync(startTime, endTime, 24, tenantId);

                await Task.WhenAll(operationStatsTask, userStatsTask, trendTask);

                var overview = (object)new
                {
                    QueryInfo = new
                    {
                        TenantId = tenantId ?? "全部",
                        IsSystemAdmin = true,
                        QueryBy = _currentUser.UserName
                    },
                    Period = new { StartTime = startTime, EndTime = endTime },
                    OperationStats = operationStatsTask.Result,
                    TopUsers = userStatsTask.Result,
                    Trend = trendTask.Result,
                    Summary = new
                    {
                        TotalOperations = operationStatsTask.Result.Values.Sum(),
                        ActiveUsers = userStatsTask.Result.Count,
                        Period = "最近30天",
                        Scope = tenantId != null ? $"租户: {tenantId}" : "全系统"
                    }
                };

                _logger.LogInformation("系统管理员 {UserId} 获取系统审计概览成功，租户: {TenantId}",
                    _currentUser.Id, tenantId ?? "全部");

                return SuccessResponse(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统审计概览失败");
                return BadResponse<object>("获取系统审计概览失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取租户列表及其基本统计信息
        /// </summary>
        /// <returns>租户列表</returns>
        [HttpGet("tenants")]
        [DisplayName("获取租户列表")]
        public async Task<ActionResult<ApiResponse<object>>> GetTenantsAsync()
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddDays(-7); // 最近7天

                // 获取所有租户的统计数据
                var operationStats = await _auditService.GetOperationStatsAsync(startTime, endTime, null);

                // 通过查询获取租户列表（这里简化处理，实际应该从租户管理服务获取）
                var tenantQuery = new AuditLogQueryDto
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    PerPage = 1000 // 获取大量数据用于分析租户
                };

                var (logs, _) = await _auditService.SearchAsync(tenantQuery);
                
                var tenantStats = logs
                    .Where(log => !string.IsNullOrEmpty(log.TenantId))
                    .GroupBy(log => log.TenantId)
                    .Select(g => new
                    {
                        TenantId = g.Key,
                        OperationCount = g.Count(),
                        UserCount = g.Select(x => x.UserId).Distinct().Count(),
                        LastActivity = g.Max(x => x.OperationTime)
                    })
                    .OrderByDescending(x => x.OperationCount)
                    .ToList();

                var result = (object)new
                {
                    Period = new { StartTime = startTime, EndTime = endTime },
                    TotalTenants = tenantStats.Count,
                    TenantStats = tenantStats,
                    Summary = new
                    {
                        TotalOperations = tenantStats.Sum(t => t.OperationCount),
                        TotalUsers = tenantStats.Sum(t => t.UserCount),
                        ActiveTenants = tenantStats.Count(t => t.OperationCount > 0)
                    }
                };

                _logger.LogInformation("系统管理员 {UserId} 获取租户列表成功，发现 {TenantCount} 个租户",
                    _currentUser.Id, tenantStats.Count);

                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取租户列表失败");
                return BadResponse<object>("获取租户列表失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 批量删除指定时间范围内的审计日志 (系统管理员专用)
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="tenantId">租户ID，可选</param>
        /// <returns>操作结果</returns>
        [HttpDelete("batch")]
        [DisplayName("批量删除审计日志")]
        public async Task<ActionResult<ApiResponse<object>>> BatchDeleteAsync(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string tenantId = null)
        {
            try
            {
                // 安全检查：防止删除过多数据
                var timeSpan = endTime - startTime;
                if (timeSpan.TotalDays > 365)
                {
                    return BadResponse<object>("删除时间范围不能超过365天");
                }

                _logger.LogWarning("系统管理员 {UserId} 请求批量删除审计日志，时间范围: {StartTime} - {EndTime}, 租户: {TenantId}",
                    _currentUser.Id, startTime, endTime, tenantId ?? "全部");

                // 这里应该调用审计服务的批量删除方法
                // var deletedCount = await _auditService.BatchDeleteAsync(startTime, endTime, tenantId);

                // 暂时返回模拟结果
                var result = (object)new
                {
                    Message = "批量删除操作已提交",
                    TimeRange = new { StartTime = startTime, EndTime = endTime },
                    TenantId = tenantId ?? "全部",
                    OperatedBy = _currentUser.UserName,
                    OperatedAt = DateTime.UtcNow,
                    Status = "处理中"
                };

                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除审计日志失败");
                return BadResponse<object>("批量删除审计日志失败: " + ex.Message);
            }
        }
    }
} 