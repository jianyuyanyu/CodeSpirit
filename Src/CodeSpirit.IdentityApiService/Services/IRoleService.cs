using CodeSpirit.IdentityApi.Controllers.Dtos;

namespace CodeSpirit.IdentityApi.Services
{
    public interface IRoleService
    {
        Task BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos);
        Task<RoleDto> CreateRoleAsync(RoleCreateDto createDto);
        Task DeleteRoleAsync(string id);
        Task<RoleDto> GetRoleByIdAsync(string id);
        Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryDto queryDto);
        Task UpdateRoleAsync(string id, RoleUpdateDto updateDto);
    }
}