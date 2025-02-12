using CodeSpirit.IdentityApi.Controllers.Dtos.Role;

namespace CodeSpirit.IdentityApi.Services
{
    /// <summary>
    /// 角色服务接口，提供角色管理相关的业务逻辑操作
    /// </summary>
    public interface IRoleService
    {
        /// <summary>
        /// 批量导入角色信息
        /// </summary>
        /// <param name="importDtos">角色导入数据列表</param>
        Task BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos);

        /// <summary>
        /// 创建新角色
        /// </summary>
        /// <param name="createDto">角色创建数据传输对象</param>
        /// <returns>创建成功的角色信息</returns>
        Task<RoleDto> CreateRoleAsync(RoleCreateDto createDto);

        /// <summary>
        /// 删除指定角色
        /// </summary>
        /// <param name="id">角色ID</param>
        Task DeleteRoleAsync(string id);

        /// <summary>
        /// 根据ID获取角色详细信息
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>角色详细信息</returns>
        Task<RoleDto> GetRoleByIdAsync(string id);

        /// <summary>
        /// 分页获取角色列表
        /// </summary>
        /// <param name="queryDto">查询条件（包含分页、排序和搜索参数）</param>
        /// <returns>角色列表和总记录数</returns>
        Task<(List<RoleDto> roles, int total)> GetRolesAsync(RoleQueryDto queryDto);

        /// <summary>
        /// 更新角色信息
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <param name="updateDto">角色更新数据传输对象</param>
        Task UpdateRoleAsync(string id, RoleUpdateDto updateDto);
    }
}