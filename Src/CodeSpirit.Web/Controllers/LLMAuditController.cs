using System.ComponentModel;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Audit.Services.LLM;
using CodeSpirit.Audit.Services.LLM.Dtos;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers
{
    /// <summary>
    /// LLM审计日志控制器
    /// 提供LLM交互的审计日志查询、统计和分析功能
    /// </summary>
    [DisplayName("LLM审计")]
    [Navigation(Icon = "fa-solid fa-brain", PlatformType = PlatformType.Both)]
    [NoAudit("LLM审计日志控制器不需要审计")]
    public class LLMAuditController : ApiControllerBase
    {
        private readonly ILLMAuditService _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<LLMAuditController> _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="auditService">LLM审计服务</param>
        /// <param name="currentUser">当前用户服务</param>
        /// <param name="logger">日志记录器</param>
        public LLMAuditController(
            ILLMAuditService auditService,
            ICurrentUser currentUser,
            ILogger<LLMAuditController> logger)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }
        
        /// <summary>
        /// 获取LLM审计日志列表
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet]
        [DisplayName("获取LLM审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<Audit.Models.LLM.LLMAuditLog>>>> GetLLMAuditLogs([FromQuery] LLMAuditQueryDto queryDto)
        {
            try
            {
                // 自动设置当前租户ID，确保数据隔离
                queryDto.TenantId = _currentUser.TenantId;

                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logList = logs.ToList();
                var pageList = new PageList<Audit.Models.LLM.LLMAuditLog>(logList, (int)total);

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM审计日志列表成功，返回 {Count} 条记录",
                    _currentUser.TenantId, _currentUser.Id, logList.Count);

                return SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM审计日志列表失败");
                return BadResponse<PageList<Audit.Models.LLM.LLMAuditLog>>("获取LLM审计日志列表失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 导出LLM审计日志列表（用于导出功能）
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>审计日志列表</returns>
        [HttpGet("Export")]
        [DisplayName("导出LLM审计日志列表")]
        public async Task<ActionResult<ApiResponse<PageList<Audit.Models.LLM.LLMAuditLog>>>> Export([FromQuery] LLMAuditQueryDto queryDto)
        {
            try
            {
                // 自动设置当前租户ID
                queryDto.TenantId = _currentUser.TenantId;
                
                // 设置导出限制
                const int MaxExportLimit = 10000;
                queryDto.PerPage = MaxExportLimit;
                queryDto.Page = 1;
                
                var (logs, total) = await _auditService.SearchAsync(queryDto);
                
                var logList = logs.ToList();
                var pageList = new PageList<Audit.Models.LLM.LLMAuditLog>(logList, (int)total);

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 导出LLM审计日志成功，共 {Count} 条记录",
                    _currentUser.TenantId, _currentUser.Id, logList.Count);

                return logList.Count == 0 
                    ? BadResponse<PageList<Audit.Models.LLM.LLMAuditLog>>("没有数据可供导出") 
                    : SuccessResponse(pageList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出LLM审计日志失败");
                return BadResponse<PageList<Audit.Models.LLM.LLMAuditLog>>("导出LLM审计日志失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM审计日志详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>审计日志详情</returns>
        [HttpGet("{id}")]
        [DisplayName("获取LLM审计日志详情")]
        public async Task<ActionResult<ApiResponse<Audit.Models.LLM.LLMAuditLog>>> Detail(string id)
        {
            try
            {
                var log = await _auditService.GetByIdAsync(id);
                
                if (log == null)
                {
                    return BadResponse<Audit.Models.LLM.LLMAuditLog>($"未找到ID为 {id} 的LLM审计日志");
                }
                
                // 检查租户权限
                if (log.TenantId != _currentUser.TenantId)
                {
                    return BadResponse<Audit.Models.LLM.LLMAuditLog>("该日志不属于当前租户");
                }
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM审计日志详情成功，日志ID: {LogId}",
                    _currentUser.TenantId, _currentUser.Id, id);

                return SuccessResponse(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM审计日志详情失败，ID: {Id}", id);
                return BadResponse<Audit.Models.LLM.LLMAuditLog>("获取LLM审计日志详情失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM使用统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>使用统计</returns>
        [HttpGet("stats/usage")]
        [DisplayName("获取LLM使用统计")]
        public async Task<ActionResult<ApiResponse<LLMUsageStatsDto>>> GetUsageStatsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddDays(-30);
                var end = endTime ?? DateTime.UtcNow;
                
                var stats = await _auditService.GetUsageStatsAsync(start, end, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM使用统计成功，时间范围: {StartTime} - {EndTime}",
                    _currentUser.TenantId, _currentUser.Id, start, end);
                
                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM使用统计失败");
                return BadResponse<LLMUsageStatsDto>("获取LLM使用统计失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM成本统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>成本统计</returns>
        [HttpGet("stats/cost")]
        [DisplayName("获取LLM成本统计")]
        public async Task<ActionResult<ApiResponse<LLMCostStatsDto>>> GetCostStatsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddDays(-30);
                var end = endTime ?? DateTime.UtcNow;
                
                var stats = await _auditService.GetCostStatsAsync(start, end, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM成本统计成功，时间范围: {StartTime} - {EndTime}",
                    _currentUser.TenantId, _currentUser.Id, start, end);
                
                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM成本统计失败");
                return BadResponse<LLMCostStatsDto>("获取LLM成本统计失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM质量统计
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>质量统计</returns>
        [HttpGet("stats/quality")]
        [DisplayName("获取LLM质量统计")]
        public async Task<ActionResult<ApiResponse<LLMQualityStatsDto>>> GetQualityStatsAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddDays(-30);
                var end = endTime ?? DateTime.UtcNow;
                
                var stats = await _auditService.GetQualityStatsAsync(start, end, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM质量统计成功，时间范围: {StartTime} - {EndTime}",
                    _currentUser.TenantId, _currentUser.Id, start, end);
                
                return SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM质量统计失败");
                return BadResponse<LLMQualityStatsDto>("获取LLM质量统计失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM使用趋势
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">时间间隔(小时)</param>
        /// <returns>使用趋势</returns>
        [HttpGet("stats/trend")]
        [DisplayName("获取LLM使用趋势")]
        public async Task<ActionResult<ApiResponse<Dictionary<DateTime, long>>>> GetUsageTrendAsync(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            [FromQuery] int interval = 24)
        {
            try
            {
                var start = startTime ?? DateTime.UtcNow.AddDays(-30);
                var end = endTime ?? DateTime.UtcNow;
                
                var trend = await _auditService.GetUsageTrendAsync(start, end, interval, _currentUser.TenantId);
                
                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM使用趋势成功，时间范围: {StartTime} - {EndTime}, 间隔: {Interval}小时",
                    _currentUser.TenantId, _currentUser.Id, start, end, interval);
                
                return SuccessResponse(trend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM使用趋势失败");
                return BadResponse<Dictionary<DateTime, long>>("获取LLM使用趋势失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// 获取LLM审计概览
        /// </summary>
        /// <returns>审计概览信息</returns>
        [HttpGet("overview")]
        [DisplayName("获取LLM审计概览")]
        public async Task<ActionResult<ApiResponse<object>>> GetOverviewAsync()
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddDays(-30); // 最近30天

                // 并行获取各项统计数据，提高性能
                var usageStatsTask = _auditService.GetUsageStatsAsync(startTime, endTime, _currentUser.TenantId);
                var costStatsTask = _auditService.GetCostStatsAsync(startTime, endTime, _currentUser.TenantId);
                var qualityStatsTask = _auditService.GetQualityStatsAsync(startTime, endTime, _currentUser.TenantId);
                var trendTask = _auditService.GetUsageTrendAsync(startTime, endTime, 24, _currentUser.TenantId);

                await Task.WhenAll(usageStatsTask, costStatsTask, qualityStatsTask, trendTask);

                var usageStats = usageStatsTask.Result;
                var costStats = costStatsTask.Result;
                var qualityStats = qualityStatsTask.Result;
                var trend = trendTask.Result;

                var overview = (object)new
                {
                    TenantInfo = new
                    {
                        TenantId = _currentUser.TenantId,
                        TenantName = _currentUser.TenantName ?? "未知租户",
                        IsCurrentTenant = true
                    },
                    Period = new { StartTime = startTime, EndTime = endTime },
                    UsageStats = usageStats,
                    CostStats = costStats,
                    QualityStats = qualityStats,
                    Trend = trend,
                    Summary = new
                    {
                        TotalInteractions = usageStats.TotalInteractions,
                        TotalCost = costStats.TotalCost,
                        SuccessRate = usageStats.SuccessRate,
                        AverageQualityScore = qualityStats.AverageQualityScore,
                        Period = "最近30天",
                        DataScope = "当前租户"
                    }
                };

                _logger.LogInformation("租户 {TenantId} 用户 {UserId} 获取LLM审计概览成功",
                    _currentUser.TenantId, _currentUser.Id);

                return SuccessResponse(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取LLM审计概览失败");
                return BadResponse<object>("获取LLM审计概览失败: " + ex.Message);
            }
        }
    }
}

