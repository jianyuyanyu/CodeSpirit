// Controllers/AuthController.cs
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
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
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var (success, message, token, user) = await _authService.LoginAsync(model.UserName, model.Password);
            if (success)
            {
                return Ok(new { token, user });
            }
            return Unauthorized(new { message });
        }
    }
}