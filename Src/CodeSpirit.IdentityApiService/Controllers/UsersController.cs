using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Utilities;
using System.Linq.Dynamic.Core.Exceptions;
using CodeSpirit.IdentityApi.Repositories;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [DisplayName("用户管理")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }


        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> GetUsers([FromQuery] UserQueryDto queryDto)
        {
            var users = await _userRepository.GetUsersAsync(queryDto);
            return SuccessResponse(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
        {
            var userDto = await _userRepository.GetUserByIdAsync(id);
            return SuccessResponse(userDto);
        }


        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto createUserDto)
        {
            var (result, userId) = await _userRepository.CreateUserAsync(createUserDto);
            if (!result.Succeeded)
            {
                return BadResponse<UserDto>(message: result.Errors.FirstOrDefault()?.Description);
            }

            var createdUserDto = await _userRepository.GetUserByIdAsync(userId);

            return SuccessResponseWithCreate<UserDto>(nameof(GetUser), createdUserDto);
        }


        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateUser(string id, UpdateUserDto updateUserDto)
        {
            var result = await _userRepository.UpdateUserAsync(id, updateUserDto);
            if (!result.Succeeded)
            {
                // 检查具体的错误描述
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "更新用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            var response = new ApiResponse<string>(0, "用户更新成功！", null);
            return Ok(response);
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(string id)
        {
            var result = await _userRepository.DeleteUserAsync(id);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "禁用用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }
            return SuccessResponse<string>();
        }


        // 额外：分配角色给用户
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRoles(string id, [FromBody] List<string> roles)
        {
            var result = await _userRepository.AssignRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "角色分配失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色分配成功。" });
        }


        // 额外：移除用户的角色
        [HttpDelete("{id}/roles")]
        public async Task<IActionResult> RemoveRoles(string id, [FromBody] List<string> roles)
        {
            var result = await _userRepository.RemoveRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "角色移除失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色移除成功。" });
        }


        /// <summary>
        /// 合并激活和禁用：通过参数 isActive 设置激活(true) 或禁用(false)
        /// PUT: /api/Users/{id}/setActive?isActive=true/false
        /// </summary>
        [HttpPut("{id}/setActive")]
        public async Task<ActionResult<ApiResponse<string>>> SetActiveStatus(string id, [FromQuery] bool isActive)
        {
            var result = await _userRepository.SetActiveStatusAsync(id, isActive);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "更新用户状态失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            var status = isActive ? "激活" : "禁用";
            var responseMsg = $"用户已{status}成功！";
            var response = new ApiResponse<string>(0, responseMsg, null);

            return Ok(response);
        }


        /// <summary>
        /// 随机生成新密码并重置（无需管理员输入新密码）
        /// POST: /api/Users/{id}/resetRandomPassword
        /// </summary>
        [HttpPost("{id}/resetRandomPassword")]
        [Operation("重置密码", "download", "/api/users/$id/export", "确定要导出此用户吗？")]
        public async Task<ActionResult<ApiResponse<string>>> ResetRandomPassword(string id)
        {
            var (success, newPassword) = await _userRepository.ResetRandomPasswordAsync(id);
            if (!success)
            {
                return BadRequest(new ApiResponse<string>(1, "密码重置失败！", null));
            }

            var response = new ApiResponse<string>(0, "密码已重置成功！", newPassword);
            return Ok(response);
        }

        /// <summary>
        /// 解除用户锁定
        /// PUT: /api/Users/{id}/unlock
        /// </summary>
        [HttpPut("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var result = await _userRepository.UnlockUserAsync(id);
            if (!result.Succeeded)
            {
                var errorDescription = result.Errors != null && result.Errors.Any()
                    ? result.Errors.First().Description
                    : "解除锁定失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "用户已成功解锁。" });
        }

        // 自定义操作：导出用户
        [HttpPost("{id}/export")]
        [Operation("导出", "download", "/api/users/$id/export", "确定要导出此用户吗？")]
        public async Task<IActionResult> ExportUser(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 实现导出逻辑
            // 例如，将用户数据导出为 CSV 文件
            var csv = $"Id,Username,Email,IsActive,Role\n{user.Id},{user.Username},{user.Email},{user.IsActive}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"{user.Username}.csv");
        }

    }
}