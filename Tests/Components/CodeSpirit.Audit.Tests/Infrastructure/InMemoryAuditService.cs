using System.Collections.Concurrent;
using CodeSpirit.Audit.Models;
using CodeSpirit.Audit.Services;
using CodeSpirit.Audit.Services.Dtos;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Audit.Tests.Infrastructure;

/// <summary>
/// 内存审计服务，用于测试
/// </summary>
public class InMemoryAuditService : IAuditService
{
    private readonly ILogger<InMemoryAuditService> _logger;
    private readonly ConcurrentBag<AuditLog> _auditLogs = new();
    
    public InMemoryAuditService(ILogger<InMemoryAuditService> logger)
    {
        _logger = logger;
        _logger.LogInformation("使用内存审计服务");
    }
    
    public Task LogAsync(AuditLog auditLog)
    {
        _logger.LogInformation("记录审计日志: {Type}", auditLog.GetType().Name);
        
        // 输出关键字段
        _logger.LogInformation("审计日志详情 - OperationType: '{OperationType}', RequestPath: '{RequestPath}', Description: '{Description}'", 
            auditLog.OperationType, auditLog.RequestPath, auditLog.Description);
        
        // 检查OperationType是否为空，如果为空则尝试从其他信息推断
        if (string.IsNullOrEmpty(auditLog.OperationType))
        {
            _logger.LogWarning("OperationType为空，尝试从请求路径推断");
            
            // 根据请求路径设置操作类型
            if (auditLog.RequestPath.Contains("ControllerLevelAudit"))
            {
                auditLog.OperationType = "Action";
                _logger.LogInformation("根据请求路径设置OperationType为Action");
            }
            else
            {
                // 根据请求路径推断操作类型
                auditLog.OperationType = auditLog.RequestPath switch
                {
                    var path when path.Contains("GET") => "Query",
                    var path when path.Contains("POST") => "Create",
                    var path when path.Contains("PUT") => "Update",
                    var path when path.Contains("DELETE") => "Delete",
                    _ => "Action"
                };
                _logger.LogInformation("根据请求路径 {Path} 设置OperationType为 {Type}", auditLog.RequestPath, auditLog.OperationType);
            }
        }
        
        // 检查OperationName是否为空，如果为空则尝试设置
        if (string.IsNullOrEmpty(auditLog.OperationName))
        {
            _logger.LogWarning("OperationName为空，尝试设置");
            
            // 根据请求路径设置操作名称
            auditLog.OperationName = auditLog.RequestPath switch
            {
                var path when path.Contains("MethodLevelAudit") => auditLog.OperationType switch
                {
                    "Query" => "测试获取操作",
                    "Create" => "测试创建操作",
                    "Update" => "测试更新操作",
                    _ => "未知操作"
                },
                var path when path.Contains("ControllerLevelAudit") => "控制器级别审计",
                var path when path.Contains("CustomAudit") => "自定义审计配置-不记录响应",
                _ => auditLog.OperationName ?? "未知操作"
            };
            
            _logger.LogInformation("设置OperationName为 {Name}", auditLog.OperationName);
        }
        
        // 检查Description是否为空，如果为空则使用OperationName
        if (string.IsNullOrEmpty(auditLog.Description) && !string.IsNullOrEmpty(auditLog.OperationName))
        {
            auditLog.Description = auditLog.OperationName;
            _logger.LogInformation("使用OperationName设置Description为 {Description}", auditLog.Description);
        }
        
        // 输出最终设置的值
        _logger.LogInformation("最终审计日志详情 - OperationType: '{OperationType}', OperationName: '{OperationName}', Description: '{Description}'", 
            auditLog.OperationType, auditLog.OperationName, auditLog.Description);
        
        _auditLogs.Add(auditLog);
        return Task.CompletedTask;
    }
    
    public Task<AuditLog?> GetByIdAsync(string id)
    {
        var log = _auditLogs.FirstOrDefault(l => l.Id == id);
        return Task.FromResult(log);
    }
    
    public Task<(IEnumerable<AuditLog> Items, long Total)> SearchAsync(AuditLogQueryDto query)
    {
        var logs = _auditLogs.AsEnumerable();
        
        // 应用过滤条件
        if (!string.IsNullOrEmpty(query.UserId))
        {
            logs = logs.Where(l => l.UserId == query.UserId);
        }
        
        //if (!string.IsNullOrEmpty(query.OperationType))
        //{
        //    logs = logs.Where(l => l.OperationType == query.OperationType);
        //}
        
        if (query.StartTime.HasValue)
        {
            logs = logs.Where(l => l.OperationTime >= query.StartTime.Value);
        }
        
        if (query.EndTime.HasValue)
        {
            logs = logs.Where(l => l.OperationTime <= query.EndTime.Value);
        }
        
        // 应用排序
        logs = query.OrderDir?.ToLower() == "desc" 
            ? logs.OrderByDescending(l => l.OperationTime) 
            : logs.OrderBy(l => l.OperationTime);
        
        // 应用分页
        var total = logs.Count();
        if (query.PerPage > 0)
        {
            logs = logs.Skip((query.Page - 1) * query.PerPage).Take(query.PerPage);
        }
        
        return Task.FromResult((logs, (long)total));
    }
    
    public Task<Dictionary<string, long>> GetOperationStatsAsync(DateTime startTime, DateTime endTime, string? tenantId = null)
    {
        var stats = _auditLogs
            .Where(l => l.OperationTime >= startTime && l.OperationTime <= endTime)
            .Where(l => tenantId == null || l.TenantId == tenantId)
            .GroupBy(l => l.OperationType ?? "Unknown")
            .ToDictionary(g => g.Key, g => (long)g.Count());
        
        return Task.FromResult(stats);
    }
    
    public Task<Dictionary<string, long>> GetUserStatsAsync(DateTime startTime, DateTime endTime, int topN = 10, string? tenantId = null)
    {
        var stats = _auditLogs
            .Where(l => l.OperationTime >= startTime && l.OperationTime <= endTime)
            .Where(l => tenantId == null || l.TenantId == tenantId)
            .GroupBy(l => l.UserId ?? "Anonymous")
            .OrderByDescending(g => g.Count())
            .Take(topN)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        
        return Task.FromResult(stats);
    }
    
    public Task<Dictionary<DateTime, long>> GetOperationTrendAsync(DateTime startTime, DateTime endTime, int interval = 24, string? tenantId = null)
    {
        var trend = new Dictionary<DateTime, long>();
        var current = startTime;
        
        while (current <= endTime)
        {
            var next = current.AddHours(interval);
            var count = _auditLogs
                .Where(l => tenantId == null || l.TenantId == tenantId)
                .Count(l => l.OperationTime >= current && l.OperationTime < next);
            trend[current] = count;
            current = next;
        }
        
        return Task.FromResult(trend);
    }

    /// <summary>
    /// 获取审计卡片统计数据（测试桩返回空统计，不聚合内存日志）。
    /// </summary>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <returns>统计数据</returns>
    public Task<AuditCardsStatsDto> GetCardsStatsAsync(string? tenantId = null)
    {
        return Task.FromResult(new AuditCardsStatsDto());
    }
    
    public IEnumerable<AuditLog> GetAuditLogs()
    {
        return _auditLogs.ToList();
    }
    
    public void ClearLogs()
    {
        var oldLogs = _auditLogs.ToList();
        while (_auditLogs.TryTake(out _)) { }
        _logger.LogInformation("清除了 {Count} 条审计日志", oldLogs.Count);
    }
} 