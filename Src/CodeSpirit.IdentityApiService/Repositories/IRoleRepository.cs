using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IRoleRepository
    {
        Task<ApplicationRole> GetRoleByIdAsync(string id);
        Task CreateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds);
        Task UpdateRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds);
        Task DeleteRoleAsync(ApplicationRole role);
        Task AssignPermissionsToRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds);
        Task RemovePermissionsFromRoleAsync(ApplicationRole role, IEnumerable<int> permissionIds);
        Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto);
        Task<List<string>> GetUserIdsByRoleId(string id);
    }
}
