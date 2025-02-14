using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos.Profile;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.IdentityApi.Controllers
{
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
        public async Task<ActionResult<ApiResponse<ProfileDto>>> GetProfile()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            {
                return Unauthorized(new ApiResponse<ProfileDto>(401, "未登录或登录已过期", null));
            }

            UserDto userDto = await _userService.GetUserByIdAsync(_currentUser.Id.Value);
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