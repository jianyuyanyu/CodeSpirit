using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CodeSpirit.Amis.Attributes;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Extensions;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.Web.Configuration.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// 租户平台审计日志控制器
    /// 提供当前租户的审计日志查询和统计功能
    /// </summary>
    [DisplayName("审计日志")]
    [Navigation(Icon = "fa-solid fa-clipboard-list", PlatformType = PlatformType.Tenant)]
    [NoAudit("审计日志控制器不需要审计")]
    [StatisticsCards<AuditLogStatisticsConfig>]
    public class AuditLogController : ApiControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<AuditLogController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">审计服务</param>
        /// <param name="currentUser">当前用户服务</param>
        /// <param name="logger">日志记录器</param>
        public AuditLogController(
            IAuditService auditService,
            ICurrentUser currentUser,
            ILogger<AuditLogController> logger)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前租户的审计日志列表
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet]
        [DisplayName("获取审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogQueryDto queryDto)
        {
            try
            {
                // 自动设置当前租户ID，确保数据隔离
                queryDto.TenantId = _currentUser.TenantId;

                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logDtos = logs.ToDto().ToList();
                var pageList = new PageList<AuditLogDto>(logDtos, (int)total);

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取审计日志列表成功，返回 {Count} 条记录",
                    _currentUser.TenantId, _currentUser.Id, logDtos.Count);

                return SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志列表失败");
                return BadResponse<PageList<AuditLogDto>>("获取审计日志列表失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 导出当前租户的审计日志列表（用于导出功能）
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet("Export")]
        [DisplayName("导出审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<AuditLogDto>>>> Export([FromQuery] AuditLogQueryDto queryDto)
        {
            try
            {
                // 自动设置当前租户ID
                queryDto.TenantId = _currentUser.TenantId;
                
                // 设置导出限制
                const int MaxExportLimit = 10000; // 最大导出数量限制
                queryDto.PerPage = MaxExportLimit;
                queryDto.Page = 1;
                
                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logDtos = logs.ToDto().ToList();
                var pageList = new PageList<AuditLogDto>(logDtos, (int)total);

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 导出审计日志成功，共 {Count} 条记录",
                    _currentUser.TenantId, _currentUser.Id, logDtos.Count);

                return logDtos.Count == 0 ? BadResponse<PageList<AuditLogDto>>("没有数据可供导出") : SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出审计日志失败");
                return BadResponse<PageList<AuditLogDto>>("导出审计日志失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的审计日志详情
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
                    TenantId = _currentUser.TenantId, // 确保只能查看当前租户的数据
                    PerPage = 1000
                };

                var (logs, _) = await _auditService.SearchAsync(query);
                
                var log = logs.FirstOrDefault(l => l.Id == id);
                if (log == null)
                {
                    return BadResponse<AuditLogDto>($"未找到ID为 {id} 的审计日志或该日志不属于当前租户");
                }
                
                var logDto = log.ToDto();
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取审计日志详情成功，日志ID: {LogId}",
                    _currentUser.TenantId, _currentUser.Id, id);

                return SuccessResponse(logDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志详情失败，ID: {Id}", id);
                return BadResponse<AuditLogDto>("获取审计日志详情失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的操作统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>操作统计</returns>
        [HttpGet("stats/operations")]
        [DisplayName("获取操作统计")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, long>>>> GetOperationStatsAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime)
        {
            try
            {
                var stats = await _auditService.GetOperationStatsAsync(startTime, endTime, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取操作统计成功，时间范围: {StartTime} - {EndTime}",
                    _currentUser.TenantId, _currentUser.Id, startTime, endTime);

                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作统计失败");
                return BadResponse<Dictionary<string, long>>("获取操作统计失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的用户操作统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="topN">前N个用户</param>
        /// <returns>用户操作统计</returns>
        [HttpGet("stats/users")]
        [DisplayName("获取用户统计")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, long>>>> GetUserStatsAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime,
            [FromQuery] int topN = 10)
        {
            try
            {
                var stats = await _auditService.GetUserStatsAsync(startTime, endTime, topN, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取用户统计成功，时间范围: {StartTime} - {EndTime}, TopN: {TopN}",
                    _currentUser.TenantId, _currentUser.Id, startTime, endTime, topN);

                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计失败");
                return BadResponse<Dictionary<string, long>>("获取用户统计失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的操作趋势
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">时间间隔(小时)</param>
        /// <returns>操作趋势</returns>
        [HttpGet("stats/trend")]
        [DisplayName("获取操作趋势")]
        public async Task<ActionResult<ApiResponse<Dictionary<DateTime, long>>>> GetOperationTrendAsync(
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime,
            [FromQuery] int interval = 24)
        {
            try
            {
                var trend = await _auditService.GetOperationTrendAsync(startTime, endTime, interval, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取操作趋势成功，时间范围: {StartTime} - {EndTime}, 间隔: {Interval}小时",
                    _currentUser.TenantId, _currentUser.Id, startTime, endTime, interval);

                return SuccessResponse(trend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取操作趋势失败");
                return BadResponse<Dictionary<DateTime, long>>("获取操作趋势失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的审计概览
        /// </summary>
        /// <returns>审计概览信息</returns>
        [HttpGet("overview")]
        [DisplayName("获取审计概览")]
        public async Task<ActionResult<ApiResponse<object>>> GetOverviewAsync()
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddDays(-30); // 最近30天

                // 并行获取各项统计数据，提高性能
                var operationStatsTask = _auditService.GetOperationStatsAsync(startTime, endTime, _currentUser.TenantId);
                var userStatsTask = _auditService.GetUserStatsAsync(startTime, endTime, 5, _currentUser.TenantId);
                var trendTask = _auditService.GetOperationTrendAsync(startTime, endTime, 24, _currentUser.TenantId);

                await Task.WhenAll(operationStatsTask, userStatsTask, trendTask);

                var overview = (object)new
                {
                    TenantInfo = new
                    {
                        TenantId = _currentUser.TenantId,
                        TenantName = _currentUser.TenantName ?? "未知租户",
                        IsCurrentTenant = true
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
                        DataScope = "当前租户"
                    }
                };

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取审计概览成功",
                    _currentUser.TenantId, _currentUser.Id);

                return SuccessResponse(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计概览失败");
                return BadResponse<object>("获取审计概览失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前租户的审计统计报表
        /// </summary>
        /// <param name="reportType">报表类型(daily, weekly, monthly)</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>统计报表</returns>
        [HttpGet("reports/{reportType}")]
        [DisplayName("获取统计报表")]
        public async Task<ActionResult<ApiResponse<object>>> GetReportAsync(
            string reportType,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1);
                var start = startDate ?? (reportType.ToLower() switch
                {
                    "daily" => end.AddDays(-7),
                    "weekly" => end.AddDays(-28),
                    "monthly" => end.AddMonths(-6),
                    _ => end.AddDays(-30)
                });

                var interval = reportType.ToLower() switch
                {
                    "daily" => 1,
                    "weekly" => 7 * 24,
                    "monthly" => 30 * 24,
                    _ => 24
                };

                // 并行获取统计数据
                var operationStatsTask = _auditService.GetOperationStatsAsync(start, end, _currentUser.TenantId);
                var userStatsTask = _auditService.GetUserStatsAsync(start, end, 10, _currentUser.TenantId);
                var trendTask = _auditService.GetOperationTrendAsync(start, end, interval, _currentUser.TenantId);

                await Task.WhenAll(operationStatsTask, userStatsTask, trendTask);

                var report = (object)new
                {
                    ReportType = reportType,
                    Period = new { StartDate = start, EndDate = end },
                    TenantId = _currentUser.TenantId,
                    Statistics = new
                    {
                        Operations = operationStatsTask.Result,
                        Users = userStatsTask.Result,
                        Trend = trendTask.Result
                    },
                    Summary = new
                    {
                        TotalOperations = operationStatsTask.Result.Values.Sum(),
                        UniqueUsers = userStatsTask.Result.Count,
                        ReportGeneratedAt = DateTime.UtcNow,
                        GeneratedBy = _currentUser.UserName
                    }
                };

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取 {ReportType} 报表成功",
                    _currentUser.TenantId, _currentUser.Id, reportType);

                return SuccessResponse(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计报表失败，报表类型: {ReportType}", reportType);
                return BadResponse<object>("获取统计报表失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取审计日志统计卡片数据
        /// </summary>
        /// <returns>统计卡片数据</returns>
        [HttpGet("statistics/cards")]
        [DisplayName("获取统计卡片")]
        public async Task<ActionResult<ApiResponse<object>>> GetStatisticsCards()
        {
            try
            {
                var stats = await _auditService.GetCardsStatsAsync(_currentUser.TenantId);
                
                // 返回扁平化数据供卡片渲染
                object data = new
                {
                    todayTotal = stats.TodayTotal,
                    todaySuccess = stats.TodaySuccess,
                    todayFailed = stats.TodayFailed,
                    successRate = $"{stats.SuccessRate:F1}%"
                };
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取审计日志统计卡片成功",
                    _currentUser.TenantId, _currentUser.Id);
                
                return SuccessResponse<object>(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志统计卡片失败");
                return BadResponse<object>("获取统计卡片失败: " + ex.Message);
            }
        }
    }
}