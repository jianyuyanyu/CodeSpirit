// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq;

namespace CodeSpirit.IdentityApi.Controllers
{
    /// <summary>
    /// 授权控制器，处理用户登录、令牌刷新和登出功能
    /// </summary>
    [AllowAnonymous]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IClientIpService _clientIpService;

        /// <summary>
        /// 初始化授权控制器
        /// </summary>
        /// <param name="authService">授权服务</param>
        /// <param name="signInManager">登录管理器</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="clientIpService">客户端IP地址获取服务</param>
        public AuthController(
            IAuthService authService,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger,
            IClientIpService clientIpService)
        {
            _authService = authService;
            _signInManager = signInManager;
            _logger = logger;
            _clientIpService = clientIpService;
        }

        /// <summary>
        /// 用户登录接口
        /// </summary>
        /// <param name="model">登录模型</param>
        /// <returns>登录结果</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [DisplayName("用户登录")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login([FromBody] LoginModel model)
        {
            try
            {
                // 从请求头或模型中获取租户ID
                var tenantId = HttpContext.Request.Headers["TenantId"].FirstOrDefault() 
                              ?? HttpContext.Items["TenantId"]?.ToString()
                              ?? model.TenantId;

                // 在服务器端获取客户端信息
                var loginDto = new LoginDto
                {
                    UserName = model.UserName,
                    Password = model.Password,
                    TenantId = tenantId,
                    IpAddress = _clientIpService.GetClientIpAddress(HttpContext),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                var result = await _authService.LoginAsync(loginDto);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };
                return SuccessResponse(response, msg: "登录成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录异常");
                return BadResponse<AuthTokenResponse>("登录失败，请检查登录名或密码！");
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        /// <param name="refreshTokenDto">包含访问令牌和刷新令牌的请求对象</param>
        /// <returns>新的令牌信息</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [DisplayName("刷新访问令牌")]
        public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (refreshTokenDto == null || string.IsNullOrEmpty(refreshTokenDto.Token) || string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
                {
                    return BadResponse<AuthTokenResponse>("访问令牌和刷新令牌不能为空");
                }

                var result = await _authService.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken);
                if (!result.Success)
                {
                    return BadResponse<AuthTokenResponse>(result.Message);
                }

                var response = new AuthTokenResponse
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    User = result.UserInfo
                };
                return SuccessResponse(response, msg: "令牌刷新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新令牌异常");
                return BadResponse<AuthTokenResponse>("刷新令牌失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 用户登出接口
        /// </summary>
        /// <returns>登出结果</returns>
        [HttpPost("logout")]
        [Authorize]
        [DisplayName("用户登出")]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            await _signInManager.SignOutAsync();
            return SuccessResponse("退出登录成功!");
        }
    }
}