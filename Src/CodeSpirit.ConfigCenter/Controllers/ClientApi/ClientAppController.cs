using CodeSpirit.ConfigCenter.Dtos.App;
using CodeSpirit.ConfigCenter.Models;
using CodeSpirit.ConfigCenter.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CodeSpirit.ConfigCenter.Controllers.ClientApi;

/// <summary>
/// 客户端应用API控制器
/// </summary>
[Route("api/config/client/apps")]
[ApiController]
public class ClientAppController : ControllerBase
{
    private readonly IAppService _appService;
    private readonly ILogger<ClientAppController> _logger;

    public ClientAppController(
        IAppService appService,
        ILogger<ClientAppController> logger)
    {
        _appService = appService;
        _logger = logger;
    }

    /// <summary>
    /// 注册应用
    /// </summary>
    /// <param name="request">应用注册请求</param>
    /// <returns>应用注册结果</returns>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AppRegistrationResponseDto>>> RegisterApp(
        [FromBody] AppRegistrationRequestDto request)
    {
        try
        {
            _logger.LogInformation("客户端API - 注册应用 {AppId}", request.Id);
            
            // 检查应用是否已存在
            var existingApp = await _appService.GetAppAsync(request.Id);
            if (existingApp != null)
            {
                return new ApiResponse<AppRegistrationResponseDto>
                {
                    Status = 400,
                    Msg = $"应用 {request.Id} 已存在",
                    Data = new AppRegistrationResponseDto 
                    { 
                        Id = request.Id,
                        Secret = existingApp.Secret
                    }
                };
            }
            
            // 创建应用
            var app = new CreateAppDto
            {
                Id = request.Id,
                Name = request.Name ?? $"Auto registered: {request.Id}",
                Description = request.Description ?? $"通过客户端API自动注册，注册时间：{DateTime.Now}",
            };
            
            var result = await _appService.CreateAppAsync(app);
            
            return new ApiResponse<AppRegistrationResponseDto>
            {
                Status = 200,
                Msg = "应用注册成功",
                Data = new AppRegistrationResponseDto
                {
                    Id = result.Id,
                    Secret = result.Secret
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用注册失败: {AppId}", request.Id);
            return new ApiResponse<AppRegistrationResponseDto>
            {
                Status = 500,
                Msg = $"应用注册失败: {ex.Message}"
            };
        }
    }
} 