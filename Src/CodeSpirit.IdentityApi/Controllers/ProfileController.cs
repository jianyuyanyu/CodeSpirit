using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
    public class ProfileController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUser _currentUser;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            IUserService userService,
            ICurrentUser currentUser,
            SignInManager<ApplicationUser> signInManager)
        {
            _userService = userService;
            _currentUser = currentUser;
            _signInManager = signInManager;
        }

        // GET: api/identity/profile
        [HttpGet("")]
        public async Task<ActionResult<ApiResponse<object>>> GetProfile()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new ApiResponse<object>(401, "未登录或登录已过期", null));
            }

            UserDto userDto = await _userService.GetUserByIdAsync(_currentUser.Id.Value);
            if (userDto == null)
            {
                return NotFound(new ApiResponse<object>(404, "用户不存在", null));
            }

            var profile = new
            {
                id = userDto.Id,
                name = userDto.Name,
                username = userDto.UserName,
                email = userDto.Email,
                avatar = userDto.AvatarUrl,
                roles = _currentUser.Roles,
                permissions = _currentUser.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
                    .ToList()
            };

            return SuccessResponse<object>(profile);
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        /// <returns></returns>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "退出登录成功" });
        }
    }
}