using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Controllers;
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
        var (roles, total) = await _roleService.GetRolesAsync(queryDto);
        return Ok(new ApiResponse<ListData<RoleDto>>
        {
            Status = 0,
            Msg = "查询成功！",
            Data = new ListData<RoleDto>
            {
                Items = roles,
                Total = total
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetRole(string id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        return SuccessResponse(role);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(RoleCreateDto createDto)
    {
        var role = await _roleService.CreateRoleAsync(createDto);
        return SuccessResponseWithCreate<RoleDto>(nameof(GetRole), role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(string id, RoleUpdateDto updateDto)
    {
        await _roleService.UpdateRoleAsync(id, updateDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Operation("删除角色", "ajax", null, "确定要删除吗？", "typeof roleId !== 'string' && children.length == 0")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        await _roleService.DeleteRoleAsync(id);
        return SuccessResponse();
    }

    [HttpDelete("{roleId}/{id}")]
    [Operation("移除权限", "ajax", null, "确定要删除吗？", "typeof roleId === 'string'")]
    public async Task<ActionResult<ApiResponse>> RemoveRolePermission(string roleId, int id)
    {
        //await _roleService.DeleteRoleAsync(id);
        return SuccessResponse();
    }
}
