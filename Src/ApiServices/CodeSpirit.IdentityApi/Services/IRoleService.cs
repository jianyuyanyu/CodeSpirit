using CodeSpirit.Core;
using CodeSpirit.IdentityApi.Data.Models;
using CodeSpirit.IdentityApi.Dtos.Role;
using CodeSpirit.Shared.Services;

namespace CodeSpirit.IdentityApi.Services;

/// <summary>
/// 角色服务接口
/// </summary>
public interface IRoleService : IBaseCRUDIService<ApplicationRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, RoleBatchImportItemDto>, IScopedDependency
{
    /// <summary>
    /// 获取角色列表（分页）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页后的角色列表</returns>
    Task<PageList<RoleDto>> GetRolesAsync(RoleQueryDto queryDto);

    /// <summary>
    /// 获取系统角色列表（分页，仅系统租户的角色）
    /// </summary>
    /// <param name="queryDto">查询条件</param>
    /// <returns>分页后的系统角色列表</returns>
    Task<PageList<RoleDto>> GetSystemRolesAsync(RoleQueryDto queryDto);

    /// <summary>
    /// 创建系统角色（在系统租户下创建）
    /// </summary>
    /// <param name="createDto">创建角色DTO</param>
    /// <returns>创建的角色</returns>
    Task<RoleDto> CreateSystemRoleAsync(RoleCreateDto createDto);

    /// <summary>
    /// 批量导入角色
    /// </summary>
    /// <param name="importDtos">要导入的角色列表</param>
    /// <returns>导入结果，包含成功数量和失败的ID列表</returns>
    Task<(int successCount, List<string> failedIds)> BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos);
    
    /// <summary>
    /// 获取用户权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户权限列表</returns>
    Task<HashSet<string>> GetUserPermissionsAsync(long userId);
}