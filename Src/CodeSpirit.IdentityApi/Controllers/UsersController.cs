using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Controllers.Dtos.Common;
using CodeSpirit.IdentityApi.Controllers.Dtos.Role;
using CodeSpirit.IdentityApi.Controllers.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("用户管理")]
    [Page(Label = "用户管理", ParentLabel = "用户中心", Icon = "fa-solid fa-users", PermissionCode = PermissionCodes.UserManagement)]
    [Permission(code: PermissionCodes.UserManagement)]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly AuthService _authService;

        public UsersController(
            IUserService userService,
            AuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> GetUsers([FromQuery] UserQueryDto queryDto)
        {
            ListData<UserDto> users = await _userService.GetUsersAsync(queryDto);
            return SuccessResponse(users);
        }

        // GET: api/Users/Export
        [HttpGet("Export")]
        public async Task<ActionResult<ApiResponse<ListData<UserDto>>>> Export([FromQuery] UserQueryDto queryDto)
        {
            // 设置导出时的分页参数
            const int MaxExportLimit = 10000; // 最大导出数量限制
            queryDto.PerPage = MaxExportLimit;
            queryDto.Page = 1;

            // 获取用户数据
            ListData<UserDto> users = await _userService.GetUsersAsync(queryDto);

            // 如果数据为空则返回错误信息
            return users.Items.Count == 0 ? BadResponse<ListData<UserDto>>("没有数据可供导出") : SuccessResponse(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Detail(long id)
        {
            UserDto userDto = await _userService.GetUserByIdAsync(id);
            return SuccessResponse(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto createUserDto)
        {
            (Microsoft.AspNetCore.Identity.IdentityResult result, long? userId) = await _userService.CreateUserAsync(createUserDto);
            if (!result.Succeeded)
            {
                return BadResponse<UserDto>(message: result.Errors.FirstOrDefault()?.Description);
            }

            UserDto createdUserDto = await _userService.GetUserByIdAsync(userId.Value);
            return SuccessResponseWithCreate<UserDto>(nameof(Detail), createdUserDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateUser(long id, UpdateUserDto updateUserDto)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "更新用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            return Ok(new ApiResponse<string>(0, "用户更新成功！", null));
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(long id)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.DeleteUserAsync(id);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "禁用用户失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }
            return SuccessResponse<string>();
        }

        // POST: api/Users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRoles(long id, [FromBody] List<string> roles)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.AssignRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "角色分配失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色分配成功。" });
        }

        // DELETE: api/Users/{id}/roles
        [HttpDelete("{id}/roles")]
        public async Task<IActionResult> RemoveRoles(long id, [FromBody] List<string> roles)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.RemoveRolesAsync(id, roles);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "角色移除失败！";
                return BadRequest(new { msg = errorDescription });
            }

            return Ok(new { msg = "角色移除成功。" });
        }

        // PUT: /api/Users/{id}/setActive?isActive=true/false
        [HttpPut("{id}/setActive")]
        public async Task<ActionResult<ApiResponse<string>>> SetActiveStatus(long id, [FromQuery] bool isActive)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.SetActiveStatusAsync(id, isActive);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "更新用户状态失败！";
                return BadRequest(new ApiResponse<string>(1, errorDescription, null));
            }

            string status = isActive ? "激活" : "禁用";
            return Ok(new ApiResponse<string>(0, $"用户已{status}成功！", null));
        }

        // POST: /api/Users/{id}/resetRandomPassword
        [HttpPost("{id}/resetRandomPassword")]
        [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true")]
        public async Task<ActionResult<ApiResponse<string>>> ResetRandomPassword(long id)
        {
            (bool success, string newPassword) = await _userService.ResetRandomPasswordAsync(id);
            return !success ? (ActionResult<ApiResponse<string>>)BadRequest(new ApiResponse<string>(1, "密码重置失败！", null)) : (ActionResult<ApiResponse<string>>)Ok(new ApiResponse<string>(0, "密码已重置成功！", newPassword));
        }

        // PUT: /api/Users/{id}/unlock
        [HttpPut("{id}/unlock")]
        [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
        public async Task<IActionResult> UnlockUser(long id)
        {
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userService.UnlockUserAsync(id);
            if (!result.Succeeded)
            {
                string errorDescription = result.Errors.FirstOrDefault()?.Description ?? "解除锁定失败！";
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

        // POST: api/Users/{id}/impersonate
        [HttpPost("{id}/impersonate")]
        [Operation("模拟登录", "ajax", null, "确定要模拟此用户登录吗？", "isActive == true", Redirect = "/impersonate?token=${token}")]
        public async Task<ActionResult<ApiResponse<object>>> ImpersonateUser(long id)
        {
            // Check if current user is Admin
            if (!User.IsInRole("Admin"))
            {
                return BadResponse<object>("只有超级管理员可以使用模拟登录功能！");
            }

            UserDto user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return BadResponse<object>("用户不存在！");
            }

            if (!user.IsActive)
            {
                return BadResponse<object>("无法模拟已禁用的用户！");
            }

            (bool success, string message, string token, UserDto userInfo) = await _authService.ImpersonateLoginAsync(user.UserName);
            return !success ? BadResponse<object>(message) : SuccessResponse<object>(new { token, userInfo });
        }

        // POST: api/Users/batch/import
        [HttpPost("batch/import")]
        public async Task<ActionResult<ApiResponse<string>>> BatchImport([FromBody] BatchImportDtoBase<UserBatchImportItemDto> importDto)
        {
            int count = await _userService.BatchImportUsersAsync(importDto.ImportData);
            return SuccessResponse($"成功批量导入了 {count} 个用户！");
        }

        // POST: api/Users/batch/delete
        [HttpPost("batch/delete")]
        [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true)]
        public async Task<ActionResult<ApiResponse<string>>> BatchDelete([FromBody] BatchDeleteDto<long> request)
        {
            (int successCount, List<string> failedUserNames) = await _userService.BatchDeleteUsersAsync(request.Ids);

            if (failedUserNames.Any())
            {
                string failedMessage = $"成功删除 {successCount} 个用户，但以下用户删除失败: {string.Join(", ", failedUserNames)}";
                return SuccessResponse(failedMessage);
            }

            return SuccessResponse($"成功删除 {successCount} 个用户！");
        }
    }
}
