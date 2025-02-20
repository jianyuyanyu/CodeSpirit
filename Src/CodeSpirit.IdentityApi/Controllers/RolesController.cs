using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Constants;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.IdentityApi.Services;
using CodeSpirit.Shared.Dtos.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

[DisplayName("角色管理")]
[Page(Label = "角色管理", ParentLabel = "用户中心", Icon = "fa-solid fa-user-shield", Permission = PermissionCodes.RoleManagement)]
[Permission(code: PermissionCodes.RoleManagement)]
public class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageList<RoleDto>>>> GetRoles([FromQuery] RoleQueryDto queryDto)
    {
        PageList<RoleDto> result = await _roleService.GetRolesAsync(queryDto);
        return SuccessResponse(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(long id)
    {
        RoleDto role = await _roleService.GetAsync(id);
        return SuccessResponse(role);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(RoleCreateDto createDto)
    {
        RoleDto roleDto = await _roleService.CreateAsync(createDto);
        return SuccessResponseWithCreate<RoleDto>(nameof(GetRole), roleDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(long id, RoleUpdateDto updateDto)
    {
        await _roleService.UpdateAsync(id, updateDto);
        return SuccessResponse();
    }

    [HttpDelete("{id}")]
    [Operation("删除角色", "ajax", null, "确定要删除此角色吗？", "permissionIds.length == 0")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        await _roleService.DeleteAsync(id);
        return SuccessResponse();
    }

    /// <summary>
    /// 批量导入角色
    /// </summary>
    /// <param name="importDto">批量导入角色 DTO 列表</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch/import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchImportDtoBase<RoleBatchImportItemDto> importDto)
    {
        (int successCount, List<string> failedIds) = await _roleService.BatchImportRolesAsync(importDto.ImportData);

        return failedIds.Count > 0
            ? SuccessResponse($"角色批量导入完成。成功：{successCount}个，失败：{failedIds.Count}个。失败的角色：{string.Join(", ", failedIds)}")
            : SuccessResponse($"角色批量导入成功，共导入{successCount}个角色");
    }
}
