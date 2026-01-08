using CodeSpirit.ConfigCenter.Dtos.Config;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Threading.Tasks;
using CodeSpirit.Audit.Attributes;

namespace CodeSpirit.ConfigCenter.Controllers.ClientApi;

/// <summary>
/// 客户端配置API控制器
/// </summary>
[Route("api/config/client/config")]
[ApiController]
[AllowAnonymous]
[NoAudit("配置中心客户端API频繁调用，不需要记录审计日志")]
public class ClientConfigController : ControllerBase
{
    private readonly IConfigItemService _configItemService;
    private readonly ILogger<ClientConfigController> _logger;

    public ClientConfigController(
        IConfigItemService configItemService,
        ILogger<ClientConfigController> logger)
    {
        _configItemService = configItemService;
        _logger = logger;
    }

    /// <summary>
    /// 获取应用配置集合
    /// </summary>
    /// <param name="appId">应用ID</param>
    /// <returns>应用配置集合</returns>
    [HttpGet("{appId}")]
    [DisplayName("获取应用配置")]
    public async Task<ActionResult<ApiResponse<ConfigItemsExportDto>>> GetAppConfig(string appId)
    {
        try
        {
            _logger.LogInformation("客户端API - 获取应用 {AppId} 的配置", appId);
                
            ConfigItemsExportDto configs = await _configItemService.GetAppConfigsWithInheritanceAsync(appId);
                
            return new ApiResponse<ConfigItemsExportDto>
            {
                Status = 200,
                Msg = "Success",
                Data = configs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取应用配置失败: {AppId}", appId);
            return new ApiResponse<ConfigItemsExportDto>
            {
                Status = 500,
                Msg = $"获取配置失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 验证客户端连接
    /// </summary>
    [HttpGet("ping")]
    [DisplayName("验证连接")]
    public ActionResult<ApiResponse<object>> Ping()
    {
        return new ApiResponse<object>
        {
            Status = 200,
            Msg = "Connected",
            Data = new { Timestamp = DateTime.UtcNow }
        };
    }
} 