using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

[Route("api/[controller]")]
[ApiController]
[DisplayName("角色管理")]
[Page(Label = "角色管理", ParentLabel = "用户中心", Icon = "fa-solid fa-user-group")]
public class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ListData<RoleDto>>>> GetRoles([FromQuery] RoleQueryDto queryDto)
    {
        (List<RoleDto> roles, int total) = await _roleService.GetRolesAsync(queryDto);
        return SuccessResponse(new ListData<RoleDto>
        {
            Items = roles,
            Total = total
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(string id)
    {
        RoleDto role = await _roleService.GetRoleByIdAsync(id);
        return SuccessResponse(role);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(RoleCreateDto createDto)
    {
        RoleDto role = await _roleService.CreateRoleAsync(createDto);
        return SuccessResponseWithCreate<RoleDto>(nameof(GetRole), role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(string id, RoleUpdateDto updateDto)
    {
        await _roleService.UpdateRoleAsync(id, updateDto);
        return SuccessResponse();
    }

    [HttpDelete("{id}")]
    [Operation("删除角色", "ajax", null, "确定要删除此角色吗？", "typeof roleId !== 'string' && permissionIds.length == 0")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        await _roleService.DeleteRoleAsync(id);
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
        await _roleService.BatchImportRolesAsync(importDto.ImportData);
        return SuccessResponse();
    }
}
