using CodeSpirit.ConfigCenter.Dtos.Client;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Services;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.ConfigCenter.Controllers;

/// <summary>
/// 客户端连接管理控制器
/// </summary>
[DisplayName("客户端连接")]
[Navigation(Icon = "fa-solid fa-plug")]
public class ClientConnectionsController : ApiControllerBase
{
    private readonly IClientTrackingService _clientTrackingService;
    private readonly ILogger<ClientConnectionsController> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientTrackingService">客户端跟踪服务</param>
    /// <param name="logger">日志记录器</param>
    public ClientConnectionsController(
        IClientTrackingService clientTrackingService,
        ILogger<ClientConnectionsController> logger)
    {
        _clientTrackingService = clientTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有在线客户端
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <returns>在线客户端列表</returns>
    [HttpGet]
    public ActionResult<ApiResponse<List<ClientConnectionDto>>> GetAllClients([FromQuery] GetClientConnectionsQueryDto query)
    {
        IEnumerable<ClientConnection> clients;

        if (!string.IsNullOrEmpty(query.AppId) && !string.IsNullOrEmpty(query.Environment))
        {
            clients = _clientTrackingService.GetConnectionsByAppAndEnvironment(query.AppId, query.Environment);
        }
        else if (!string.IsNullOrEmpty(query.AppId))
        {
            clients = _clientTrackingService.GetConnectionsByApp(query.AppId);
        }
        else
        {
            clients = _clientTrackingService.GetAllConnections();
        }

        var result = clients.Select(c => new ClientConnectionDto
        {
            ConnectionId = c.ConnectionId,
            ClientId = c.ClientId,
            AppId = c.AppId,
            Environment = c.Environment,
            IpAddress = c.IpAddress,
            HostName = c.HostName,
            Version = c.Version,
            ConnectedTime = c.ConnectedTime,
            LastActiveTime = c.LastActiveTime,
            Status = (DateTime.UtcNow - c.LastActiveTime).TotalMinutes > 1 ? "可能断开" : "活跃"
        }).ToList();

        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取客户端连接统计信息
    /// </summary>
    /// <returns>客户端连接统计信息</returns>
    [HttpGet("stats")]
    public object GetConnectionStats()
    {
        var allConnections = _clientTrackingService.GetAllConnections().ToList();

        var stats = new
        {
            TotalConnections = allConnections.Count,
            UniqueApps = allConnections.Select(c => c.AppId).Where(a => !string.IsNullOrEmpty(a)).Distinct().Count(),
            UniqueClients = allConnections.Select(c => c.ClientId).Where(c => !string.IsNullOrEmpty(c)).Distinct().Count(),
            ByApp = allConnections
                .Where(c => !string.IsNullOrEmpty(c.AppId))
                .GroupBy(c => c.AppId)
                .Select(g => new
                {
                    AppId = g.Key,
                    ConnectionCount = g.Count(),
                    EnvironmentCount = g.Select(c => c.Environment).Distinct().Count()
                })
                .OrderByDescending(x => x.ConnectionCount)
                .ToList()
        };

        return stats;
    }
} 