// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Auth;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    [AllowAnonymous]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(IAuthService authService, SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _signInManager = signInManager;
        }

        /// <summary>
        /// 用户登录方法。
        /// </summary>
        /// <param name="model">登录模型，包含用户名和密码。</param>
        /// <returns>登录结果。</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResult>>> Login([FromBody] LoginModel model)
        {
            (bool success, string message, string token, UserDto user) = await _authService.LoginAsync(model.UserName, model.Password);
            if (success)
            {
                LoginResult result = new()
                {
                    Token = token,
                };
                return SuccessResponse(result);
            }
            return BadResponse<LoginResult>(message);
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        /// <returns></returns>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            await _signInManager.SignOutAsync();
            return SuccessResponse("退出登录成功!");
        }
    }
}