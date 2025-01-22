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
        [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true")]
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
        [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
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

        [HttpPatch("quickSave")]
        public async Task<ActionResult<ApiResponse<string>>> QuickSaveUsers([FromBody] QuickSaveRequestDto request)
        {
            // 1. 确保请求数据有效
            if (request?.Rows == null || !request.Rows.Any())
            {
                return BadRequest(new ApiResponse<string>(1, "请求数据无效或为空", null));
            }

            // 2. 获取需要更新的用户ID列表
            var userIdsToUpdate = request.Rows.Select(row => row.Id).ToList();
            var usersToUpdate = await _userRepository.GetUsersByIdsAsync(userIdsToUpdate);
            if (usersToUpdate.Count != userIdsToUpdate.Count)
            {
                return NotFound(new ApiResponse<string>(1, "部分用户未找到", null));
            }

            // 3. 执行批量更新：更新 `rowsDiff` 中的变化字段
            foreach (var rowDiff in request.RowsDiff)
            {
                var user = usersToUpdate.FirstOrDefault(u => u.Id == rowDiff.Id);
                if (user != null)
                {
                    // 更新变化字段（仅更新在 rowsDiff 中的字段）
                    if (rowDiff.IsActive.HasValue)
                    {
                        user.IsActive = rowDiff.IsActive.Value;
                    }

                    // 你可以根据需求增加更多字段的更新
                }
            }

            // 4. 保存更新结果
            var updateResult = await _userRepository.SaveChangesAsync();
            if (updateResult == 0)
            {
                return BadRequest(new ApiResponse<string>(1, "批量更新失败", null));
            }

            // 5. 返回成功响应
            return Ok(new ApiResponse<string>(0, "批量更新成功", null));
        }

        #region 数据统计
        [HttpGet("user-growth")]
        public async Task<IActionResult> GetUserGrowth()
        {
            var data = await _userRepository.GetUserGrowthAsync(DateTime.Now.AddDays(-7), DateTime.Now);
            return Ok(new { dates = data.Select(x => x.Date.ToString("yyyy-MM-dd")).ToArray(), userCounts = data.Select(x => x.UserCount).ToArray() });
        }

        [HttpGet("active-users")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var data = await _userRepository.GetActiveUsersAsync(DateTime.Now.AddDays(-7), DateTime.Now);
            return Ok(new { dates = data.Select(x => x.Date.ToString("yyyy-MM-dd")).ToArray(), activeUserCounts = data.Select(x => x.ActiveUserCount).ToArray() });
        }

        #endregion

    }
}