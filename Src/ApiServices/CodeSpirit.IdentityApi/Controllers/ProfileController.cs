using CodeSpirit.Authorization;
using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Audit.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Dtos.Profile;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Module("default", displayName: "默认")]
    [Navigation(Hidden = true)]
    [NoAudit("个人资料控制器不需要审计")]
    public class ProfileController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUser _currentUser;

        public ProfileController(
            IUserService userService,
            ICurrentUser currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }

        // GET: api/identity/profile
        [HttpGet("")]
        [DisplayName("获取个人资料")]
        public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new ApiResponse<ProfileDto>(401, "未登录或登录已过期", null));
            }

            // 使用专门的方法查询用户信息，避免租户过滤器影响
            // 对于获取当前用户自己的资料，应该忽略租户过滤器
            UserDto userDto = await _userService.GetUserByIdIgnoreFiltersAsync(_currentUser.Id.Value);
            if (userDto == null)
            {
                return NotFound(new ApiResponse<ProfileDto>(404, "用户不存在", null));
            }

            ProfileDto profile = new()
            {
                Id = userDto.Id,
                Name = userDto.Name,
                UserName = userDto.UserName,
                Email = userDto.Email,
                AvatarUrl = userDto.AvatarUrl,
                PhoneNumber = userDto.PhoneNumber,
                Roles = _currentUser.Roles,
                Permissions = _currentUser.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
            };

            return SuccessResponse(profile);
        }
    }
}