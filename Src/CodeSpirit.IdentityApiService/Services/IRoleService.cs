using CodeSpirit.IdentityApi.Controllers.Dtos;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IRoleService
    {
        Task<RoleDto> CreateRoleAsync(RoleCreateDto createDto);
        Task DeleteRoleAsync(string id);
        Task<RoleDto> GetRoleByIdAsync(string id);
        Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryDto queryDto);
        Task RemovePermissionsFromRoleAsync(string id, IEnumerable<int> permissionIds);
        Task UpdateRoleAsync(string id, RoleUpdateDto updateDto);
    }
}