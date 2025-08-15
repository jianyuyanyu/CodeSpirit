using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Dtos;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.User;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Data;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.System;

/// <summary>
/// 系统平台用户管理控制器
/// </summary>
[DisplayName("用户管理")]
[Navigation(Icon = "fa-solid fa-users-gear", PlatformType = PlatformType.System)]
public class SystemUsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IDataFilter _dataFilter;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userService">用户服务</param>
    /// <param name="authService">认证服务</param>
    /// <param name="dataFilter">数据筛选器</param>
    public SystemUsersController(
        IUserService userService,
        IAuthService authService,
        IDataFilter dataFilter)
    {
        _userService = userService;
        _authService = authService;
        _dataFilter = dataFilter;
    }

    /// <summary>
    /// 获取系统用户列表（跨租户查询）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统用户分页列表</returns>
    [HttpGet]
    [DisplayName("获取系统用户列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemUserDto>>>> GetSystemUsers([FromQuery] SystemUserQueryDto queryDto)
    {
        PageList<SystemUserDto> users = await _userService.GetSystemUsersAsync(queryDto);
        return SuccessResponse(users);
    }

    /// <summary>
    /// 获取按租户分组的用户统计信息
    /// </summary>
    /// <returns>租户用户统计列表</returns>
    [HttpGet("tenant-statistics")]
    [DisplayName("获取租户用户统计")]
    public async Task<ActionResult<ApiResponse<List<TenantUserStatisticsDto>>>> GetTenantUserStatistics()
    {
        List<TenantUserStatisticsDto> statistics = await _userService.GetUsersByTenantStatisticsAsync();
        return SuccessResponse(statistics);
    }

    /// <summary>
    /// 导出系统用户列表
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>用户数据</returns>
    [HttpGet("export")]
    [DisplayName("导出系统用户列表")]
    public async Task<ActionResult<ApiResponse<PageList<SystemUserDto>>>> ExportSystemUsers([FromQuery] SystemUserQueryDto queryDto)
    {
        // 设置导出时的分页参数
        const int MaxExportLimit = 50000; // 系统级导出数量上限更高
        queryDto.PerPage = MaxExportLimit;
        queryDto.Page = 1;

        // 获取用户数据
        var users = await _userService.GetSystemUsersAsync(queryDto);

        // 如果数据为空则返回错误信息
        return users.Items.Count == 0 ? BadResponse<PageList<SystemUserDto>>("没有数据可供导出") : SuccessResponse(users);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取用户详情")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Detail(long id)
    {
        // 禁用多租户筛选器来获取跨租户用户详情
        using (_dataFilter.Disable<IMultiTenant>())
        {
            UserDto userDto = await _userService.GetAsync(id);
            return SuccessResponse(userDto);
        }
    }

    // /// <summary>
    // /// 将用户转移到指定租户
    // /// </summary>
    // /// <param name="id">用户ID</param>
    // /// <param name="targetTenantId">目标租户ID</param>
    // /// <returns>转移结果</returns>
    // [HttpPut("{id}/transfer")]
    // [Operation("转移租户", "form", null, "确定要将此用户转移到指定租户吗？")]
    // [DisplayName("转移用户租户")]
    // public async Task<ActionResult<ApiResponse<UserDto>>> TransferUserToTenant(long id, [FromBody] string targetTenantId)
    // {
    //     // 禁用多租户筛选器来跨租户转移用户
    //     using (_dataFilter.Disable<IMultiTenant>())
    //     {
    //         UserDto userDto = await _userService.TransferUserToTenantAsync(id, targetTenantId);
    //         return SuccessResponse(userDto, "用户租户转移成功！");
    //     }
    // }

    ///// <summary>
    ///// 创建系统用户
    ///// </summary>
    ///// <param name="createUserDto">创建用户DTO</param>
    ///// <returns>创建结果</returns>
    //[HttpPost]
    //[DisplayName("创建系统用户")]
    //public async Task<ActionResult<ApiResponse<UserDto>>> CreateSystemUser(CreateUserDto createUserDto)
    //{
    //    var userDto = await _userService.CreateAsync(createUserDto);
    //    return SuccessResponseWithCreate<UserDto>(nameof(Detail), userDto);
    //}

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="updateUserDto">更新用户DTO</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新用户信息")]
    public async Task<ActionResult<ApiResponse>> UpdateUser(long id, UpdateUserDto updateUserDto)
    {
        // 禁用多租户筛选器来跨租户更新用户
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await _userService.UpdateUserAsync(id, updateUserDto);
            return SuccessResponse();
        }
    }

    ///// <summary>
    ///// 删除用户
    ///// </summary>
    ///// <param name="id">用户ID</param>
    ///// <returns>删除结果</returns>
    //[HttpDelete("{id}")]
    //[Operation("删除", "ajax", null, "确定要删除此用户吗？")]
    //[DisplayName("删除用户")]
    //public async Task<ActionResult<ApiResponse>> DeleteUser(long id)
    //{
    //    // 禁用多租户筛选器来跨租户删除用户
    //    using (_dataFilter.Disable<IMultiTenant>())
    //    {
    //        await _userService.DeleteAsync(id);
    //        return SuccessResponse();
    //    }
    //}

    /// <summary>
    /// 分配用户角色
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="roles">角色列表</param>
    /// <returns>分配结果</returns>
    [HttpPost("{id}/roles")]
    [DisplayName("分配用户角色")]
    public async Task<ActionResult<ApiResponse>> AssignRoles(long id, [FromBody] List<string> roles)
    {
        // 禁用多租户筛选器来跨租户分配角色
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await _userService.AssignRolesAsync(id, roles);
            return SuccessResponse();
        }
    }

    /// <summary>
    /// 移除用户角色
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="roles">角色列表</param>
    /// <returns>移除结果</returns>
    [HttpDelete("{id}/roles")]
    [DisplayName("移除用户角色")]
    public async Task<ActionResult<ApiResponse>> RemoveRoles(long id, [FromBody] List<string> roles)
    {
        // 禁用多租户筛选器来跨租户移除角色
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await _userService.RemoveRolesAsync(id, roles);
            return SuccessResponse();
        }
    }

    /// <summary>
    /// 设置用户激活状态
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="isActive">是否激活</param>
    /// <returns>设置结果</returns>
    [HttpPut("{id}/setActive")]
    [DisplayName("设置用户激活状态")]
    public async Task<ActionResult<ApiResponse>> SetActiveStatus(long id, [FromQuery] bool isActive)
    {
        // 禁用多租户筛选器来跨租户设置用户状态
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await _userService.SetActiveStatusAsync(id, isActive);
            string status = isActive ? "激活" : "禁用";
            return SuccessResponse($"用户已{status}成功！");
        }
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>重置结果</returns>
    [HttpPost("{id}/resetPassword")]
    [Operation("重置密码", "ajax", null, "确定要重置密码吗？", "isActive == true",
        FeedbackTitle = "重置密码结果",
        FeedbackBodyTpl = @"{
            'type': 'form',
            'body': [
                {
                    'type': 'tpl',
                    'tpl': '<div class=""alert alert-warning""><i class=""fa fa-exclamation-circle""></i> <strong>注意：</strong>密码只显示一次，请及时保存!</div>',
                    'className': 'mb-3'
                },
                {
                    'type': 'button',
                    'label': '${newPassword}',
                    'icon': 'fa fa-copy',
                    'actionType': 'copy',
                    'content': '${newPassword}'
                }
            ]
        }")]
    [DisplayName("重置用户密码")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(long id)
    {
        // 禁用多租户筛选器来跨租户重置密码
        using (_dataFilter.Disable<IMultiTenant>())
        {
            string newPassword = await _userService.ResetRandomPasswordAsync(id);
            return Ok(new
            {
                message = "密码已重置成功！",
                data = new { newPassword },
            });
        }
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>解锁结果</returns>
    [HttpPut("{id}/unlock")]
    [Operation("解锁", "ajax", null, "确定要解除用户锁定吗？", "lockoutEnd != null")]
    [DisplayName("解锁用户")]
    public async Task<ActionResult<ApiResponse>> UnlockUser(long id)
    {
        // 禁用多租户筛选器来跨租户解锁用户
        using (_dataFilter.Disable<IMultiTenant>())
        {
            await _userService.UnlockUserAsync(id);
            return SuccessResponse("用户已成功解锁。");
        }
    }

    /// <summary>
    /// 批量删除用户
    /// </summary>
    /// <param name="request">批量操作请求</param>
    /// <returns>删除结果</returns>
    [HttpPost("batch/delete")]
    [Operation("批量删除", "ajax", null, "确定要批量删除?", isBulkOperation: true)]
    [DisplayName("批量删除用户")]
    public async Task<ActionResult<ApiResponse>> BatchDelete([FromBody] BatchOperationDto<long> request)
    {
        // 禁用多租户筛选器来跨租户批量删除用户
        using (_dataFilter.Disable<IMultiTenant>())
        {
            List<ApplicationUser> users = await _userService.GetUsersByIdsAsync(request.Ids);
            foreach (ApplicationUser user in users)
            {
                await _userService.DeleteAsync(user.Id);
            }
            return SuccessResponse($"成功删除 {users.Count} 个用户");
        }
    }
} 