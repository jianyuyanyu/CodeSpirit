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
    /// 批量导入角色
    /// </summary>
    /// <param name="importDtos">要导入的角色列表</param>
    /// <returns>导入结果，包含成功数量和失败的ID列表</returns>
    Task<(int successCount, List<string> failedIds)> BatchImportRolesAsync(List<RoleBatchImportItemDto> importDtos);
}