// Controllers/AuthController.cs
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data;
using CodeSpirit.IdentityApi.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        /// 用户登录方法。
        /// </summary>
        /// <param name="model">登录模型，包含用户名和密码。</param>
        /// <returns>登录结果。</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // 获取用户信息
            var user = await _userManager.FindByNameAsync(model.UserName);
            var loginLog = new LoginLog
            {
                UserId = user?.Id,
                UserName = model.UserName,
                LoginTime = DateTime.UtcNow,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                IsSuccess = false, // 默认为失败，后续根据结果更新
                FailureReason = null
            };

            if (user == null)
            {
                loginLog.FailureReason = "用户不存在。";
                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();
                return Unauthorized(new { message = "用户名或密码不正确。" });
            }

            // 检查密码
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                loginLog.IsSuccess = true;
                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                // 生成 JWT 或其他认证令牌
                var token = GenerateJwtToken(user);

                return Ok(new { token });
            }
            else
            {
                loginLog.FailureReason = result.IsLockedOut ? "账户被锁定。" : "密码不正确。";
                _context.LoginLogs.Add(loginLog);
                await _context.SaveChangesAsync();

                if (result.IsLockedOut)
                {
                    return Unauthorized(new { message = "账户被锁定，请稍后再试。" });
                }

                return Unauthorized(new { message = "用户名或密码不正确。" });
            }
        }

        /// <summary>
        /// 生成 JWT 令牌的方法。
        /// </summary>
        /// <param name="user">登录的用户。</param>
        /// <returns>JWT 令牌字符串。</returns>
        private string GenerateJwtToken(ApplicationUser user)
        {
            // 实现您的 JWT 生成逻辑
            // 这可能涉及到使用 JwtSecurityTokenHandler 和相关配置
            return "your_jwt_token";
        }
    }
}