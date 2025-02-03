using CodeSpirit.Amis.Attributes;
using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [DisplayName("用户管理")]
    [Page(Label = "用户管理", ParentLabel = "用户中心", Icon = "fa-solid fa-user")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> GetUsers([FromQuery] UserQueryDto queryDto)
        {
            var users = await _userService.GetUsersAsync(queryDto);
            return SuccessResponse(users);
        }

        // GET: api/Users/Export
        [HttpGet("Export")]
        public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> Export([FromQuery] UserQueryDto queryDto)
        {
            //暂时写死1万数据
            queryDto.PerPage = 10000;
            queryDto.Page = 1;
            var users = await _userService.GetUsersAsync(queryDto);
            return SuccessResponse(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
        {
            var userDto = await _userService.GetUserByIdAsync(id);
            return SuccessResponse(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto createUserDto)
        {
            var (result, userId) = await _userService.CreateUserAsync(createUserDto);
            if (!result.Succeeded)
            {
                return BadResponse<UserDto>(message: result.Errors.FirstOrDefault()?.Description);
            }

            var createdUserDto = await _userService.GetUserByIdAsync(userId);
            return SuccessResponseWithCreate<UserDto>(nameof(GetUser), createdUserDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateUser(string id, UpdateUserDto updateUserDto)
        {
            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "更新用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            return Ok(new ApiResponse<string>(0, "用户更新成功！", null));
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(string id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "禁用用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }
            return SuccessResponse<string>();
        }

        // POST: api/Users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRoles(string id, [FromBody] List<string> roles)
        {
            var result = await _userService.AssignRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "角色分配失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色分配成功。" });
        }

        // DELETE: api/Users/{id}/roles
        [HttpDelete("{id}/roles")]
        public async Task<IActionResult> RemoveRoles(string id, [FromBody] List<string> roles)
        {
            var result = await _userService.RemoveRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "角色移除失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色移除成功。" });
        }

        // PUT: /api/Users/{id}/setActive?isActive=true/false
        [HttpPut("{id}/setActive")]
        public async Task<ActionResult<ApiResponse<string>>> SetActiveStatus(string id, [FromQuery] bool isActive)
        {
            var result = await _userService.SetActiveStatusAsync(id, isActive);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "更新用户状态失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            var status = isActive ? "激活" : "禁用";
            return Ok(new ApiResponse<string>(0, $"用户已{status}成功！", null));
        }

        // POST: /api/Users/{id}/resetRandomPassword
        [HttpPost("{id}/resetRandomPassword")]
        [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true")]
        public async Task<ActionResult<ApiResponse<string>>> ResetRandomPassword(string id)
        {
            var (success, newPassword) = await _userService.ResetRandomPasswordAsync(id);
            if (!success)
            {
                return BadRequest(new ApiResponse<string>(1, "密码重置失败！", null));
            }

            return Ok(new ApiResponse<string>(0, "密码已重置成功！", newPassword));
        }

        // PUT: /api/Users/{id}/unlock
        [HttpPut("{id}/unlock")]
        [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var result = await _userService.UnlockUserAsync(id);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors.FirstOrDefault()?.Description ?? "解除锁定失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "用户已成功解锁。" });
        }

        // PATCH: /api/Users/quickSave
        [HttpPatch("quickSave")]
        public async Task<ActionResult<ApiResponse>> QuickSaveUsers([FromBody] QuickSaveRequestDto request)
        {
            await _userService.QuickSaveUsersAsync(request);
            return SuccessResponse();
        }
    }
}
