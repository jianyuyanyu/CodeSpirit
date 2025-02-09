// Controllers/AuthController.cs
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ApiControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
                var result = new LoginResult()
                {
                    Token = token,
                };
                return SuccessResponse(result);
            }
            return BadResponse<LoginResult>(message);
        }
    }
}