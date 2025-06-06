using CodeSpirit.Audit.Helpers;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace CodeSpirit.Audit.Controllers;

/// <summary>
/// 审计日志控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[DisplayName("审计日志")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditLogsController> _logger;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuditLogsController(
        IAuditService auditService,
        ILogger<AuditLogsController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取审计日志列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogQueryDto query)
    {
        try
        {
            var (items, total) = await _auditService.SearchAsync(query);
            return Ok(new
            {
                Items = items,
                Total = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计日志列表失败");
            return StatusCode(500, "获取审计日志列表失败");
        }
    }
    
    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogById(string id)
    {
        try
        {
            var log = await _auditService.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计日志详情失败");
            return StatusCode(500, "获取审计日志详情失败");
        }
    }
    
    /// <summary>
    /// 获取操作统计
    /// </summary>
    [HttpGet("stats/operations")]
    public async Task<IActionResult> GetOperationStats([FromQuery] DateTime? startTime, [FromQuery] DateTime? endTime)
    {
        try
        {
            var start = startTime ?? DateTime.Now.AddDays(-30);
            var end = endTime ?? DateTime.Now;
            
            var stats = await _auditService.GetOperationStatsAsync(start, end);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作统计失败");
            return StatusCode(500, "获取操作统计失败");
        }
    }
    
    /// <summary>
    /// 获取用户操作统计
    /// </summary>
    [HttpGet("stats/users")]
    public async Task<IActionResult> GetUserStats(
        [FromQuery] DateTime? startTime, 
        [FromQuery] DateTime? endTime,
        [FromQuery] int topN = 10)
    {
        try
        {
            var start = startTime ?? DateTime.Now.AddDays(-30);
            var end = endTime ?? DateTime.Now;
            
            var stats = await _auditService.GetUserStatsAsync(start, end, topN);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户操作统计失败");
            return StatusCode(500, "获取用户操作统计失败");
        }
    }
    
    /// <summary>
    /// 获取操作趋势
    /// </summary>
    [HttpGet("stats/trend")]
    public async Task<IActionResult> GetOperationTrend(
        [FromQuery] DateTime? startTime, 
        [FromQuery] DateTime? endTime,
        [FromQuery] string interval = "day")
    {
        try
        {
            var start = startTime ?? DateTime.Now.AddDays(-30);
            var end = endTime ?? DateTime.Now;
            
            // 转换间隔
            int intervalHours = 24; // 默认按天
            switch (interval.ToLower())
            {
                case "hour":
                    intervalHours = 1;
                    break;
                case "day":
                    intervalHours = 24;
                    break;
                case "week":
                    intervalHours = 168; // 7天
                    break;
                case "month":
                    intervalHours = 720; // 30天
                    break;
                default:
                    intervalHours = 24;
                    break;
            }
            
            var trend = await _auditService.GetOperationTrendAsync(start, end, intervalHours);
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取操作趋势失败");
            return StatusCode(500, "获取操作趋势失败");
        }
    }
} 