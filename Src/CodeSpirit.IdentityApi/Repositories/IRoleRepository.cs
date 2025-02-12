using CodeSpirit.IdentityApi.Controllers.Dtos.Role;
using CodeSpirit.IdentityApi.Data.Models;

namespace CodeSpirit.IdentityApi.Repositories
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// 批量插入角色
        /// </summary>
        /// <param name="roles">角色列表</param>
        Task BulkInsertRolesAsync(IEnumerable<ApplicationRole> roles);

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="role">角色实体</param>
        /// <param name="permissionIds">权限ID列表</param>
        Task CreateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds);

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="role">角色实体</param>
        Task DeleteRoleAsync(ApplicationRole role);

        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>角色实体</returns>
        Task<ApplicationRole> GetRoleByIdAsync(long id);

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <returns>角色列表和总数</returns>
        Task<(List<ApplicationRole>, int)> GetRolesAsync(RoleQueryDto queryDto);

        /// <summary>
        /// 根据角色名称列表获取角色
        /// </summary>
        /// <param name="roleNames">角色名称列表</param>
        /// <returns>角色列表</returns>
        Task<List<ApplicationRole>> GetRolesByNamesAsync(List<string> roleNames);

        /// <summary>
        /// 获取拥有指定角色的用户ID列表
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>用户ID列表</returns>
        Task<List<long>> GetUserIdsByRoleId(long id);

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="role">角色实体</param>
        /// <param name="permissionIds">权限ID列表</param>
        Task UpdateRoleAsync(ApplicationRole role, IEnumerable<string> permissionIds);
    }
}
