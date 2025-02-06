using CodeSpirit.IdentityApi.Controllers.Dtos;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    public interface IRoleRepository
    {
        Task BulkInsertRolesAsync(IEnumerable<ApplicationRole> roles);
        Task CreateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds);
        Task DeleteRoleAsync(ApplicationRole role);
        Task<ApplicationRole> GetRoleByIdAsync(string id);
        Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto);
        Task<List<string>> GetUserIdsByRoleId(string id);
        Task UpdateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds);
    }
}
