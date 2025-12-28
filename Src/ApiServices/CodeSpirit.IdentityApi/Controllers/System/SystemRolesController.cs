using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Enums;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.IdentityApi.Resources;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Localization.Resources;
using CodeSpirit.Navigation.Resources;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace CodeSpirit.IdentityApi.Controllers.System;

/// <summary>
/// 系统平台角色管理控制器
/// </summary>
[DisplayName("角色管理")]
[Navigation(Icon = "fa-solid fa-user-shield", PlatformType = PlatformType.System, TitleResourceKey = "Controller.SystemRoles", TitleResourceType = typeof(NavigationResources))]
public class SystemRolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleService">角色服务</param>
    public SystemRolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>
    /// 获取系统角色列表（仅系统租户的角色）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>系统角色分页列表</returns>
    [HttpGet]
    [DisplayName("获取系统角色列表")]
    public async Task<ActionResult<ApiResponse<PageList<RoleDto>>>> GetSystemRoles([FromQuery] RoleQueryDto queryDto)
    {
        // 调用专门的系统角色查询方法，确保只查询系统平台的角色
        PageList<RoleDto> result = await _roleService.GetSystemRolesAsync(queryDto);
        return SuccessResponse(result);
    }

    /// <summary>
    /// 获取系统角色详情
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>角色详情</returns>
    [HttpGet("{id}")]
    [DisplayName("获取系统角色详情")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetSystemRole(long id)
    {
        RoleDto role = await _roleService.GetAsync(id);
        return SuccessResponse(role);
    }

    /// <summary>
    /// 创建系统角色
    /// </summary>
    /// <param name="createDto">创建角色DTO</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [DisplayName("创建系统角色")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateSystemRole(RoleCreateDto createDto)
    {
        // 调用专门的系统角色创建方法，确保在系统租户下创建角色
        RoleDto roleDto = await _roleService.CreateSystemRoleAsync(createDto);
        return SuccessResponseWithCreate<RoleDto>(nameof(GetSystemRole), roleDto);
    }

    /// <summary>
    /// 更新系统角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <param name="updateDto">更新角色DTO</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [DisplayName("更新系统角色")]
    public async Task<ActionResult<ApiResponse>> UpdateSystemRole(long id, RoleUpdateDto updateDto)
    {
        await _roleService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    /// <summary>
    /// 删除系统角色
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [Operation("删除", "ajax", null, "确定要删除此系统角色吗？", "permissionIds.length == 0 && name !='SystemAdmin' && name !='TenantOperator' && name !='SystemAuditor'",
        LabelResourceKey = "Common.Delete",
        LabelResourceType = typeof(SharedResources),
        ConfirmTextResourceKey = "Operations.ConfirmDeleteSystemRole",
        ConfirmTextResourceType = typeof(IdentityDisplayResources))]
    [DisplayName("删除系统角色")]
    public async Task<ActionResult<ApiResponse>> DeleteSystemRole(long id)
    {
        await _roleService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 获取系统角色使用统计
    /// </summary>
    /// <returns>角色使用统计</returns>
    [HttpGet("usage-statistics")]
    [DisplayName("获取系统角色使用统计")]
    public async Task<ActionResult<ApiResponse<object>>> GetSystemRoleUsageStatistics()
    {
        try
        {
            // 这里需要在服务层实现获取系统角色使用统计的逻辑
            // 包括各角色的用户数量、权限分配情况等
            object statistics = new
            {
                Message = "系统角色使用统计功能待实现",
                Timestamp = DateTimeOffset.Now
            };

            return SuccessResponse(statistics);
        }
        catch (Exception)
        {
            return BadResponse<object>("获取系统角色使用统计失败");
        }
    }

    /// <summary>
    /// 获取系统角色权限分配情况
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>权限分配情况</returns>
    [HttpGet("{roleId}/permissions")]
    [DisplayName("获取角色权限分配")]
    public async Task<ActionResult<ApiResponse<object>>> GetSystemRolePermissions(long roleId)
    {
        try
        {
            // 这里需要在服务层实现获取角色权限的逻辑
            object permissions = new
            {
                RoleId = roleId,
                Message = "角色权限查询功能待实现",
                Timestamp = DateTimeOffset.Now
            };

            return SuccessResponse(permissions);
        }
        catch (Exception)
        {
            return BadResponse<object>("获取角色权限失败");
        }
    }

    /// <summary>
    /// 批量导入系统角色
    /// </summary>
    /// <param name="importDto">批量导入角色DTO列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/import")]
    [DisplayName("批量导入系统角色")]
    public async Task<ActionResult<ApiResponse>> BatchImportSystemRoles([FromBody] BatchImportDtoBase<RoleBatchImportItemDto> importDto)
    {
        (int successCount, List<string> failedIds) = await _roleService.BatchImportRolesAsync(importDto.ImportData);

        return failedIds.Count > 0
            ? SuccessResponse($"系统角色批量导入完成。成功：{successCount}个，失败：{failedIds.Count}个。失败的角色：{string.Join(", ", failedIds)}")
            : SuccessResponse($"系统角色批量导入成功，共导入{successCount}个角色");
    }
} 