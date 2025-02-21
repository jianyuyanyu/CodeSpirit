using CodeSpirit.Core;
using CodeSpirit.Core.Dtos;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers
{
    [DisplayName("用户管理")]
    [Page(Label = "用户管理", ParentLabel = "用户中心", Icon = "fa-solid fa-users")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UsersController(
            IUserService userService,
            IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PageList<UserDto>>>> GetUsers([FromQuery] UserQueryDto queryDto)
        {
            PageList<UserDto> users = await _userService.GetUsersAsync(queryDto);
            return SuccessResponse(users);
        }

        // GET: api/Users/Export
        [HttpGet("Export")]
        public async Task<ActionResult<ApiResponse<PageList<UserDto>>>> Export([FromQuery] UserQueryDto queryDto)
        {
            // 设置导出时的分页参数
            const int MaxExportLimit = 10000; // 最大导出数量限制
            queryDto.PerPage = MaxExportLimit;
            queryDto.Page = 1;

            // 获取用户数据
            PageList<UserDto> users = await _userService.GetUsersAsync(queryDto);

            // 如果数据为空则返回错误信息
            return users.Items.Count == 0 ? BadResponse<PageList<UserDto>>("没有数据可供导出") : SuccessResponse(users);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Detail(long id)
        {
            UserDto userDto = await _userService.GetAsync(id);
            return SuccessResponse(userDto);
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto createUserDto)
        {
            var userDto = await _userService.CreateAsync(createUserDto);
            return SuccessResponseWithCreate<UserDto>(nameof(Detail), userDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse>> UpdateUser(long id, UpdateUserDto updateUserDto)
        {
            await _userService.UpdateUserAsync(id, updateUserDto);
            return SuccessResponse();
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteUser(long id)
        {
            await _userService.DeleteAsync(id);
            return SuccessResponse();
        }

        // POST: api/Users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<ActionResult<ApiResponse>> AssignRoles(long id, [FromBody] List<string> roles)
        {
            await _userService.AssignRolesAsync(id, roles);
            return SuccessResponse();
        }

        // DELETE: api/Users/{id}/roles
        [HttpDelete("{id}/roles")]
        public async Task<ActionResult<ApiResponse>> RemoveRoles(long id, [FromBody] List<string> roles)
        {
            await _userService.RemoveRolesAsync(id, roles);
            return SuccessResponse();
        }

        // PUT: /api/Users/{id}/setActive
        [HttpPut("{id}/setActive")]
        public async Task<ActionResult<ApiResponse>> SetActiveStatus(long id, [FromQuery] bool isActive)
        {
            await _userService.SetActiveStatusAsync(id, isActive);
            string status = isActive ? "激活" : "禁用";
            return SuccessResponse($"用户已{status}成功！");
        }

        // POST: /api/Users/{id}/resetRandomPassword
        [HttpPost("{id}/resetRandomPassword")]
        [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true")]
        public async Task<ActionResult<ApiResponse>> ResetRandomPassword(long id)
        {
            string newPassword = await _userService.ResetRandomPasswordAsync(id);
            return SuccessResponse(newPassword);
        }

        // PUT: /api/Users/{id}/unlock
        [HttpPut("{id}/unlock")]
        [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
        public async Task<ActionResult<ApiResponse>> UnlockUser(long id)
        {
            await _userService.UnlockUserAsync(id);
            return SuccessResponse("用户已成功解锁。");
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

            UserDto user = await _userService.GetAsync(id);
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
        public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<UserBatchImportItemDto> importDto)
        {
            var (successCount, failedIds) = await _userService.BatchImportAsync(importDto.ImportData);
            return SuccessResponse($"成功批量导入了 {successCount} 个用户！");
        }

        // POST: api/Users/batch/delete
        [HttpPost("batch/delete")]
        [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true)]
        public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchDeleteDto<long> request)
        {
            var (successCount, failedIds) = await _userService.BatchDeleteAsync(request.Ids);

            if (failedIds.Any())
            {
                string failedMessage = $"成功删除 {successCount} 个用户，但以下用户删除失败: {string.Join(", ", failedIds)}";
                return SuccessResponse(failedMessage);
            }

            return SuccessResponse($"成功删除 {successCount} 个用户！");
        }
    }
}
